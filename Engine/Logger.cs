using Hexa.NET.ImGui;
using Microsoft.VisualBasic.Logging;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.IMGUIImpl;

namespace Engine
{
    public class Logger
    {
        public enum LogLevel
        {
            Debug,
            Info,
            Warn,
            Error,
            Fatal,
        }

        private static bool showLogLevel = true;
        private static bool showLogName = true;
        private static bool showLogThread = true;
        private static bool showLogTime = true;

        private static string? logLevelFormat = null;

        [ConsoleCommandVar("log_level", "the global log level. Logs below this level will not be printed (ex. log_level = Info, Debug will not be printed)")]
#if DEBUG
        public static LogLevel GlobalLogLevel = LogLevel.Debug;
#else
        public static LogLevel GlobalLogLevel = LogLevel.Warn;
#endif

        [ConsoleCommandVar("cl_show_log_settings")]
        public static bool ShowLogSettings = false;

        static Logger()
        {
            // Logs are a bit unique in that we want to always have a unique settings file so that we don't have to mess around with settings or change defaults to get things to show up.

            ReadLogSettings();
        }

        private static void ReadLogSettings()
        {
            if (!File.Exists("log_settings.txt")) return;

            using (var file = File.OpenRead("log_settings.txt"))
            {
                using (var reader = new StreamReader(file))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (line == null) break;

                        string[] split = line.Split(' ');
                        if (split.Length != 2) continue;
                        string name = split[0];
                        string value = split[1];

                        if (name == "showLogLevel")
                            bool.TryParse(value, out showLogLevel);
                        else if (name == "showLogName")
                            bool.TryParse(value, out showLogName);
                        else if (name == "showLogTime")
                            bool.TryParse(value, out showLogTime);
                        else if (name == "showLogThread")
                            bool.TryParse(value, out showLogThread);
                        else
                        {
                            if (Enum.TryParse(value, out LogLevel level))
                                settingLevels.Add(name, level);
                            else GetOrCreate("Logger").Log(LogLevel.Error, "Invalid log level {0}", value.ToString());
                        }
                    }
                }
            }
        }

        private static void WriteLogSettings()
        {
            using (var file = File.Create("log_settings.txt"))
            {
                using (var writer = new StreamWriter(file))
                {
                    writer.WriteLine("showLogLevel {0}", showLogLevel);
                    writer.WriteLine("showLogName {0}", showLogName);
                    writer.WriteLine("showLogTime {0}", showLogTime);
                    writer.WriteLine("showLogThread {0}", showLogThread);
                    foreach (var logger in loggers.Values)
                    {
                        writer.WriteLine("{0} {1}", logger.name, logger.level.ToString());
                    }
                }
            }
        }

        private static Dictionary<string, LogLevel> settingLevels = [];
        private static Dictionary<string, Logger> loggers = [];
        public static Logger InitLogger(string name, bool defaultEnabled = true, LogLevel defaultLogLevel = LogLevel.Debug)
        {
            Logger logger = new Logger(name);
            logger.enabled = defaultEnabled;
            logger.level = defaultLogLevel;

            if (settingLevels.TryGetValue(name, out LogLevel settingLevel))
                logger.level = settingLevel;

            loggers.Add(name, logger);

            return loggers[name];
        }

        public static Logger GetOrCreate(string name)
        {
            if (loggers.ContainsKey(name))
                return loggers[name];
            else return InitLogger(name);
        }

        public static Logger GetLogger(string name)
        {
            return loggers[name];
        }

        private static void RecalcFormatStr()
        {
            StringBuilder sb = new StringBuilder();
            if (showLogName)
                sb.Append("[{0}]");
            if (showLogLevel)
                sb.Append("[{1}]");
            if (showLogThread)
                sb.Append("[{2}]");
            if (showLogTime)
                sb.Append("[{3}]");
            sb.Append(" {4}");
            logLevelFormat = sb.ToString();
        }

        public static void DoImgui()
        {
            if (ShowLogSettings)
            {
                if (ImGui.Begin("Log Settings", ref ShowLogSettings))
                {
                    if (ImGui.BeginCombo("Global Log Level", GlobalLogLevel.ToString()))
                    {
                        foreach (string name in Enum.GetNames<LogLevel>())
                        {
                            if (ImGui.Selectable(name, name == GlobalLogLevel.ToString()))
                            {
                                GlobalLogLevel = Enum.Parse<LogLevel>(name);
                            }
                        }
                        ImGui.EndCombo();
                    }

                    bool anyChanged = false;
                    anyChanged |= ImGui.Checkbox("Level", ref showLogLevel);
                    ImGui.SameLine();
                    anyChanged |= ImGui.Checkbox("Name", ref showLogName);
                    anyChanged |= ImGui.Checkbox("Thread", ref showLogThread);
                    ImGui.SameLine();
                    anyChanged |= ImGui.Checkbox("Time", ref showLogTime);

                    if (anyChanged)
                        RecalcFormatStr();

                    foreach (var log in loggers.Values)
                    {
                        ImGui.PushID(log.name);
                        ImGui.Checkbox(log.name, ref log.enabled);
                        ImGui.SameLine();
                        if (ImGui.BeginCombo("Log Level", log.level.ToString()))
                        {
                            foreach (string name in Enum.GetNames<LogLevel>())
                            {
                                if (ImGui.Selectable(name, name == log.level.ToString()))
                                {
                                    log.level = Enum.Parse<LogLevel>(name);
                                    anyChanged = true;
                                }
                            }
                            ImGui.EndCombo();
                        }
                        ImGui.PopID();
                    }

                    if (anyChanged) WriteLogSettings();
                }
                ImGui.End();
            }
        }

        public List<string> totalLog;

        private StringBuilder sbuilder;

        private string name;

        private Thread mainThread;

        public LogLevel level = LogLevel.Debug;
        public bool enabled = true;

        public Logger(string name)
        {
            this.name = name;

            mainThread = Thread.CurrentThread;

            sbuilder = new StringBuilder();
            totalLog = new List<string>(512);
        }

        public void Log(LogLevel level, string format, params object?[] args)
        {
            if (!enabled || (int)level < (int)this.level || (int)level < (int)GlobalLogLevel) return;

            string str = null!;
            lock (sbuilder)
            {
                sbuilder.AppendFormat(format, args);
                str = sbuilder.ToString();
                sbuilder.Clear();
            }

            Log(level, str);
        }

        public void Log(LogLevel level, string message)
        {
            if (!enabled || (int)level < (int)this.level || (int)level < (int)GlobalLogLevel) return;

#if !DEBUG
			if (level == LogLevel.Debug)
				return;	//don't log debug logs if we're on release version
#endif
            string threadName = Thread.CurrentThread.Name;

            lock (sbuilder)
            {
                if (threadName == "" && Thread.CurrentThread == mainThread)
                    threadName = "Main Thread";

                if (logLevelFormat == null) RecalcFormatStr();
                sbuilder.AppendFormat(logLevelFormat!, name, level.ToString(), threadName, DateTime.Now.ToString("g"), message);

                string str = sbuilder.ToString();
                totalLog.Add(str);
                Console.WriteLine(str);
                sbuilder.Clear();
            }
        }
    }
}
