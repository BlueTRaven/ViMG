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

        [ConsoleCommandVar("log_level", "the global log level. Logs below this level will not be printed (ex. log_level = Info, Debug will not be printed)")]
#if DEBUG
        public static LogLevel GlobalLogLevel = LogLevel.Debug;
#else
        public static LogLevel GlobalLogLevel = LogLevel.Warn;
#endif

        [ConsoleCommandVar("cl_show_log_settings")]
        public static bool ShowLogSettings = true;

        private static Dictionary<string, Logger> loggers = new Dictionary<string, Logger>();
        public static Logger InitLogger(string name, bool defaultEnabled = true, LogLevel defaultLogLevel = LogLevel.Debug)
        {
            Logger logger = new Logger(name);
            logger.enabled = defaultEnabled;
            logger.level = defaultLogLevel;
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

        public static void DoImgui()
        {
            if (ShowLogSettings && ImGui.Begin("Log Settings", ref ShowLogSettings))
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

                foreach (var log in loggers.Values)
                {
                    ImGui.Checkbox(log.name, ref log.enabled);
                    ImGui.SameLine();
                    if (ImGui.BeginCombo("Log Level", log.level.ToString()))
                    {
                        foreach (string name in Enum.GetNames<LogLevel>())
                        {
                            if (ImGui.Selectable(name, name == log.level.ToString()))
                            {
                                log.level = Enum.Parse<LogLevel>(name);
                            }
                        }
                        ImGui.EndCombo();
                    }
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

                sbuilder.AppendFormat("[{0}][{1}][{2}][{3}] {4}", name, level.ToString(), threadName, DateTime.Now.ToString("g"), message);

                string str = sbuilder.ToString();
                totalLog.Add(str);
                Console.WriteLine(str);
                sbuilder.Clear();
            }
        }
    }
}
