using BrUtility;
using Engine.Networking;
using Engine.Networking.Messages;
using ImGuiNET;
using Microsoft.Xna.Framework;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.IMGUIImpl
{
    public static class IMGUIConsole
    {
        public class ConsoleParamException : Exception
        {
            private readonly string executingCommand;
            private readonly string paramName;
            private readonly string[] options;

            public ConsoleParamException(string executingCommand, string paramName, string[] options = null) : 
                base()
            {
                this.executingCommand = executingCommand;
                this.paramName = paramName;
                this.options = options;
            }

            public override string Message 
            {
                get 
                {
                    string baseError = "Command " + executingCommand + ": Parameter invalid. Parameter Name: " + paramName;

                    if (options != null)
                    {
                        StringBuilder optionsStr = new StringBuilder();
                        foreach (string option in options)
                        {
                            optionsStr.Append("\t");
                            optionsStr.Append(option);
                            optionsStr.Append("\n");
                        }
                        return baseError + "\nValid Options:\n" + optionsStr.ToString();
                    }
                    else return baseError;
                }
            }
        }

        private class ConsoleTraceListener : TraceListener
        {
            private readonly ConsoleTextWriter writer;

            public ConsoleTraceListener(ConsoleTextWriter writer)
            {
                this.writer = writer;
            }

            public override void Write(string? message)
            {
                writer.Write(message);
            }

            public override void WriteLine(string? message)
            {
                writer.WriteLine(message);
            }

            public override void Fail(string? message, string? detailMessage)
            {
                base.Fail(message, detailMessage);
                throw new Exception("Failed");
            }
        }
        private class ConsoleTextWriter : TextWriter
        {
            private TextWriter originalConsoleOut;
            public override Encoding Encoding => originalConsoleOut.Encoding;

            public ConsoleTextWriter(TextWriter originalConsoleOut)
            {
                this.originalConsoleOut = originalConsoleOut;
            }

            public override void Write(char value)
            {
                LogLine(new string(value, 1));
                originalConsoleOut.Write(value);
            }

            public override void Write(string value)
            {
                LogLine(value);
                originalConsoleOut.Write(value);
            }

            // Override WriteLine methods as well
            public override void WriteLine(string value)
            {
                LogLine(value);
                originalConsoleOut.WriteLine(value);
            }
        }
        private static ConsoleTextWriter textWriter;

        private const int MAX_LINES = 500;
        private const int MAX_HISTORY = 500;
        private static FastList<string> lines = new(MAX_LINES);
        private static FastList<string> commandHistory = new(MAX_HISTORY); 

        private static string editingString = "";
        private static string executingCommand = "";

        // 0 = End of buffer
        private static int historyPos = 0;

        private static bool autoScroll = true;
        private static bool scrollToBottom = false;

        private static List<(MethodInfo, ConsoleCommandAttribute)> commands = new();
        private static Dictionary<string, (MethodInfo, ConsoleCommandAttribute)> commandsByName = new();

        private static List<(FieldInfo, ConsoleCommandVarAttribute)> vars = new();
        private static Dictionary<string, (FieldInfo, ConsoleCommandVarAttribute)> varsByName = new();

        private static float lastRunTime;
        private static int lastRunLines = 0;
        private static int lastRunLines1 = 0;

        static IMGUIConsole()
        {
            if (textWriter == null)
            {
                textWriter = new ConsoleTextWriter(System.Console.Out);
                System.Console.SetOut(textWriter);
                Trace.Listeners.Clear();
                Trace.AutoFlush = true;
                Trace.Listeners.Add(new ConsoleTraceListener(textWriter));
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes()) 
                {
                    foreach (MethodInfo methodInfo in type.GetMethods())
                    {
                        if (methodInfo.IsStatic)
                        {
                            ConsoleCommandAttribute consoleCommandAttr = methodInfo.GetCustomAttribute<ConsoleCommandAttribute>();

                            if (consoleCommandAttr != null)
                            {
                                commands.Add((methodInfo, consoleCommandAttr));
                                commandsByName.Add(consoleCommandAttr.name, (methodInfo, consoleCommandAttr));
                            }
                        }
                    }

                    foreach (FieldInfo fieldInfo in type.GetFields())
                    {
                        if (fieldInfo.IsStatic)
                        {
                            ConsoleCommandVarAttribute consoleVarAttr = fieldInfo.GetCustomAttribute<ConsoleCommandVarAttribute>();

                            if (consoleVarAttr != null)
                            {
                                vars.Add((fieldInfo, consoleVarAttr));
                                varsByName.Add(consoleVarAttr.name, (fieldInfo, consoleVarAttr));
                            }
                        }
                    }
                }
            }
        }

        [ConsoleCommand("help")]
        public static void Help(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
            {
                LogLine("# Available commands:");
                foreach ((MethodInfo, ConsoleCommandAttribute) command in commands)
                {
                    LogLine("# \t" + command.Item2.name);
                }
            } 
            else
            {
                if (commandsByName.TryGetValue(parameters[1], out var command))
                {
                    LogLine("# " + command.Item2.help);
                }
                else LogLine("# No command with name " + parameters[1] + ".");
            }
        }

        [ConsoleCommand("clear", "Clears lines displayed in the console. Does not clear history.")]
        public static void Clear(string[] parameters)
        {
            lines.Clear();

            historyPos = 0;
            //TODO: history should be kept by clear command
            commandHistory.Clear();
        }

        [ConsoleCommand("run_script", "Runs a script, which is a collection of commands stored in plain-text, newline-separated format.")]
        public static void LoadScript(string[] parameters)
        {
            RequireParam(parameters, 0, "script_name");

            string scriptName = parameters[0];

            string[] allLines = File.ReadAllLines(scriptName);

            foreach (string line in allLines)
            {
                // running scripts in scripts not supported because we can EASILY deadlock ourselves...
                if (!line.StartsWith("run_script") && line != "")
                    HandleCommand(line);
            }
        }

        [ConsoleCommand("list_vars", "List console variables.")]
        public static void ListVars(string[] parameters)
        {
            foreach (var v in vars)
            {
                LogLine(v.Item2.name);
                LogLine("\t" + v.Item2.description);
            }
        }

        [ConsoleCommand("get", "Get the value of a console variable.", ConsoleCommandRunSide.Server)]
        public static void Get(string[] parameters)
        {
            RequireParam(parameters, 0, "name");
            string nameParam = parameters[0];

            if (varsByName.TryGetValue(nameParam, out var variable))
            {
                FieldInfo fieldInfo = variable.Item1;
                LogLine(nameParam + ": " + fieldInfo.GetValue(null).ToString());
            } 
            else
            {
                LogLine("[error] Could not find variable with name " + nameParam);
            }
        }

        [ConsoleCommand("set", "Set the value of a console variable.", ConsoleCommandRunSide.ServerAndClient)]
        public static void Set(string[] parameters)
        {
            RequireParam(parameters, 0, "name");
            RequireParam(parameters, 1, "value");

            string nameParam = parameters[0];

            if (varsByName.TryGetValue(nameParam, out var variable))
            {
                string valueParam = parameters[1];

                FieldInfo fieldInfo = variable.Item1;

                if (fieldInfo.FieldType == typeof(int))
                {
                    fieldInfo.SetValue(null, int.Parse(valueParam));
                } 
                else if (fieldInfo.FieldType == typeof(float))
                {
                    fieldInfo.SetValue(null, float.Parse(valueParam));
                }
                else if (fieldInfo.FieldType == typeof(bool))
                {
                    fieldInfo.SetValue(null, bool.Parse(valueParam));
                }
                else if (fieldInfo.FieldType == typeof(string))
                {
                    fieldInfo.SetValue(null, valueParam);
                }
                else if (fieldInfo.FieldType.IsEnum)
                {
                    fieldInfo.SetValue(null, Enum.Parse(fieldInfo.FieldType, valueParam));
                }

                LogLine(nameParam + ": " + fieldInfo.GetValue(null).ToString());
            }
            else
            {
                LogLine("[error] Could not find variable with name " + nameParam);
            }
        }

        [ConsoleCommand("print", "Prints a line to the console.")]
        public static void Print(string[] parameters)
        {
            if (parameters != null && parameters.Length != 0)
            {
                LogLine(parameters[0]);
            }
        }

        [ConsoleCommand("quit", "Exits the program.")]
        public static void QuitCommand(string[] parameters)
        {
            Main.Exit = true;
        }

        [ConsoleCommand("exit", "Exits the program.")]
        public static void ExitCommand(string[] parameters)
        {
            Main.Exit = true;
        }

        public static bool RequireParam(string[] parameters, int index, string paramName, string[] options = null)
        {
            if (parameters == null)
            {
                throw new ArgumentException("Command " + executingCommand + " requires at least " + (index + 1) + " parameters. Given: 0");
            }
            else if (parameters.Length <= index)
            {
                throw new ArgumentException("Command " + executingCommand + " requires at least " + (index + 1) + " parameters. Given: " + parameters.Length);
            } 
            else
            {
                if (options == null) return true;
                else
                {
                    string parameter = parameters[index];

                    foreach (string option in options)
                    {
                        if (option == parameter)
                        {
                            return true;
                        }
                    }

                    throw new ConsoleParamException(executingCommand, paramName, options);
                }
            }

            return false;
        }

        public static void OnExiting()
        {
            if (File.Exists("current_run.txt"))
                File.Copy("current_run.txt", "previous_run.txt", true);
        }

        public static FastList<string> GetHistory()
        {
            return lines;
        }

        public static unsafe void Console()
        {
            if (Main.Time > lastRunTime + 1 && lastRunLines1 != lastRunLines)
            {
                lastRunTime = (float)Main.Time;
                File.WriteAllLines("current_run.txt", commandHistory.Buffer[0..commandHistory.Length]);

                lastRunLines1 = lastRunLines;
            }

            bool shouldFocus = false;
            if (Main.inputManager.JustPressed(Microsoft.Xna.Framework.Input.Keys.OemTilde))
            {
                Options.ShowConsole = true;
                shouldFocus = true;
            }

            if (Options.ShowConsole)
            {
                if (ImGui.Begin("Console", ref Options.ShowConsole))
                {
                    float footer_height_to_reserve = ImGui.GetStyle().ItemSpacing.Y + ImGui.GetFrameHeightWithSpacing();

                    if (ImGui.BeginChild("ScrollingRegion", new(0, -footer_height_to_reserve), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
                    {
                        if (ImGui.BeginPopupContextWindow())
                        {
                            if (ImGui.Selectable("Clear"))
                            { }
                            //ClearLog();
                            ImGui.EndPopup();
                        }

                        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(4, 1)); // Tighten spacing
                                                                                                          //if (copy_to_clipboard)
                                                                                                          //    ImGui::LogToClipboard();
                        for (int i = 0; i < lines.Length; i++)
                        {
                            string item = lines[i];

                            // Normally you would store more information in your item than just a string.
                            // (e.g. make Items[] an array of structure, store color/type etc.)
                            System.Numerics.Vector4 color = new();
                            bool has_color = false;
                            if (item.StartsWith("[error] "))
                            {
                                color = new(1.0f, 0.4f, 0.4f, 1.0f);
                                has_color = true;
                            }
                            else if (item.StartsWith("# "))
                            {
                                color = new(0.8f, 0.8f, 0.8f, 1.0f);
                                has_color = true;
                            }
                            else if (item.StartsWith("<"))
                            {
                                int formatcommandStart = 1;
                                int formatcommandEnd = 0;
                                for (int j = 1; j < item.Length; j++)
                                {
                                    if (item[j] == '>')
                                    {
                                        formatcommandEnd = j;
                                        break;
                                    }
                                }

                                if (formatcommandEnd != 0)
                                {

                                    string formatcommand = item[formatcommandStart..formatcommandEnd];

                                    if (formatcommand.StartsWith("color("))
                                    {
                                        int colStart = "color(".Length;

                                        int colEnd = 0;

                                        for (int j = 0; j < formatcommand.Length; j++)
                                        {
                                            if (formatcommand[j] == ')')
                                            {
                                                colEnd = j;
                                                break;
                                            }
                                        }

                                        if (colEnd != 0)
                                        {
                                            string colStr = formatcommand[colStart..colEnd];

                                            if (uint.TryParse(colStr, System.Globalization.NumberStyles.HexNumber, null, out uint colInt))
                                            {
                                                Color xnaColor = new Color(colInt);
                                                color = xnaColor.ToVector4().ToNumerics();
                                                has_color = true;

                                                item = item[(formatcommandEnd + 2)..];
                                            }
                                        }
                                    }
                                }
                            }

                            if (has_color)
                                ImGui.PushStyleColor(ImGuiCol.Text, color);
                            ImGui.TextUnformatted(item);
                            if (has_color)
                                ImGui.PopStyleColor();
                        }

                        // Keep up at the bottom of the scroll region if we were already at the bottom at the beginning of the frame.
                        // Using a scrollbar or mouse-wheel will take away from the bottom edge.
                        if (scrollToBottom || (autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY()))
                            ImGui.SetScrollHereY(1.0f);
                        scrollToBottom = false;

                        ImGui.PopStyleVar();
                    }
                    ImGui.EndChild();
                    ImGui.Separator();

                    bool reclaim_focus = false;
                    ImGuiInputTextFlags input_text_flags = ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.EscapeClearsAll | ImGuiInputTextFlags.CallbackCompletion | ImGuiInputTextFlags.CallbackHistory;
                    if (shouldFocus)
                    {
                        ImGui.SetKeyboardFocusHere(0);
                    }
                    if (ImGui.InputText("Input", ref editingString, (uint)500, input_text_flags, Callback, (nint)null))
                    {
                        if (editingString != "")
                        {
                            HandleCommand(editingString);
                        }

                        editingString = "";
                        reclaim_focus = true;
                    }

                    // Auto-focus on window apparition
                    ImGui.SetItemDefaultFocus();
                    if (reclaim_focus)
                        ImGui.SetKeyboardFocusHere(-1); // Auto focus previous widget

                }
                ImGui.End();
            }
        }

        private static void HandleCommand(string entireLine)
        {
            LogLine("> " + entireLine);

            string[] splits = entireLine.Split(' ');
            string[] parameters = splits.Length > 1 ? splits[1..] : [];

            string commandName = splits[0];

            NetworkManager.NetworkSide netSide = Main.gameStateManager.netMode == GameStates.GameStateManager.NetworkingMode.Client ? NetworkManager.NetworkSide.Client : NetworkManager.NetworkSide.Server;
            RunCommand(commandName, netSide, parameters);            
        }

        public struct CommandReturn
        {
            public string[] output;
            public bool valid;
        }

        public static CommandReturn RunCommand(string commandName, NetworkManager.NetworkSide originatingSide, params string[] parameters)
        {
            return RunCommand(commandName, originatingSide, (Span<string>)parameters);
        }

        public static CommandReturn RunCommand(string commandName, NetworkManager.NetworkSide originatingSide, Span<string> parameters)
        {
            string[] newLines = [];
            bool valid = false;

            if (commandsByName.TryGetValue(commandName, out var command))
            {
                MethodInfo methodInfo = command.Item1;
                // Note we leave out the command itself from the strings we pass in.

                executingCommand = commandName;
                if (originatingSide == NetworkManager.NetworkSide.Client && 
                    (command.Item2.runSide == ConsoleCommandRunSide.Server || command.Item2.runSide == ConsoleCommandRunSide.ServerAndClient) && 
                    Main.gameStateManager.netMode == GameStates.GameStateManager.NetworkingMode.Client)
                {
                    // These commands are not run locally, but are instead sent to the server.
                    SyncConsoleCommandClient.Instance.SendCommand(commandName, parameters.ToArray());
                }
                else
                {
                    int before = lines.Length;
                    try
                    {
                        methodInfo.Invoke(null, [parameters.ToArray()]);
                        valid = true;
                    }
                    catch (Exception e)
                    {
                        LogLine("[error] " + e.InnerException.Message);
                    }

                    newLines = lines.Buffer[before..lines.Length];
                }
            }
            else LogLine("[error] No command with name " + commandName + ".");

            historyPos = -1;
            if (commandHistory.Length == MAX_HISTORY)
                commandHistory.RemoveAt(0);
            commandHistory.Add(editingString);

            return new CommandReturn
            {
                output = newLines,
                valid = valid,
            };
        }

        private static unsafe int Callback(ImGuiInputTextCallbackData* data)
        {
            switch (data->EventFlag)
            {
                case ImGuiInputTextFlags.CallbackCompletion:
                    {
                        char[] chars = new char[data->BufTextLen];
                        for (int i = 0; i < data->BufTextLen; i++)
                        {
                            chars[i] = (char)data->Buf[i];
                        }

                        //string entireString = new string(chars);
                        int wordEndIndex = data->CursorPos;
                        int wordStartIndex = wordEndIndex;

                        while (wordStartIndex > 0)
                        {
                            char c = editingString[wordStartIndex - 1];
                            if (c == ' ' || c == '\t' || c == ',' || c == ';')
                                break;
                            wordStartIndex--;
                        }

                        int wordLen = wordEndIndex - wordStartIndex;
                        string currentWord = editingString[wordStartIndex..wordEndIndex];

                        // Locate beginning of current word
                        //byte* word_end = data->Buf + data->CursorPos;
                        //byte* word_start = word_end;

                        //while (word_start > data->Buf)
                        //{
                        //    byte c = word_start[-1];
                        //    if (c == ' ' || c == '\t' || c == ',' || c == ';')
                        //        break;
                        //    word_start--;
                        //}

                        //int wordLen = (int)(word_end - word_start);
                        //string currentWord = new string((char*)word_start, 0, wordLen);

                        string[] commands = commandsByName.Keys.ToArray();
                        
                        // Build a list of candidates
                        List<string> candidates = new List<string>();
                        for (int i = 0; i < commands.Length; i++)
                        {
                            if (wordLen <= commands[i].Length)
                            {
                                string candidateSubstr = commands[i][0..wordLen];
                                if (candidateSubstr.ToLower() == currentWord.ToLower())
                                    candidates.Add(commands[i]);
                            }
                        }

                        if (candidates.Count == 0)
                        {
                            // No match
                            //AddLog("No match for \"%.*s\"!\n", (int)(word_end - word_start), word_start);
                        }
                        else if (candidates.Count == 1)
                        {
                            string first = editingString[0..wordStartIndex];
                            string last = editingString[wordEndIndex..];

                            string totalFirst = first + candidates[0];
                            string totalString = totalFirst + last;

                            byte[] bytes = new byte[totalString.Length];

                            for (int i = 0; i < totalString.Length; i++)
                            {
                                data->Buf[i] = (byte)totalString[i];
                            }
                            data->Buf[totalString.Length] = 0;
                            data->BufDirty = 1;
                            data->BufTextLen = totalString.Length;
                            data->CursorPos = totalFirst.Length;
                        }
                        else
                        {
                            int highestMatch = 0;
                            char compareChar = candidates[0][0];

                            int minStrLen = candidates.Min(x => x.Length);
                            for (int i = 0; i < minStrLen; i++) 
                            {
                                bool allMatched = true;
                                foreach (string candidate in candidates)
                                {
                                    if (candidate[i] != compareChar)
                                    {
                                        allMatched = false;
                                        break;
                                    }
                                }

                                if (!allMatched)
                                    break;

                                highestMatch = i;
                                compareChar = candidates[0][i + 1];
                            }

                            string highestMatchedStr = candidates[0][0..(highestMatch + 1)];

                            string first = editingString[0..wordStartIndex];
                            string last = editingString[wordEndIndex..];

                            string totalFirst = first + highestMatchedStr;
                            string totalString = totalFirst + last;

                            byte[] bytes = new byte[totalString.Length];

                            for (int i = 0; i < totalString.Length; i++)
                            {
                                data->Buf[i] = (byte)totalString[i];
                            }
                            data->Buf[totalString.Length] = 0;
                            data->BufDirty = 1;
                            data->BufTextLen = totalString.Length;
                            data->CursorPos = totalFirst.Length;

                            LogLine("# Available Matches:");
                            foreach (string candidate in candidates)
                                LogLine("# \t" + candidate);
                        }

                        break;
                    }
                case ImGuiInputTextFlags.CallbackHistory:
                    {
                        int oldHistoryPos = historyPos;

                        if (data->EventKey == ImGuiKey.UpArrow)
                            historyPos++;

                        if (data->EventKey == ImGuiKey.DownArrow)
                            historyPos--;

                        if (historyPos != oldHistoryPos)
                        {
                            if (historyPos >= commandHistory.Length)
                                historyPos = commandHistory.Length - 1;
                            if (historyPos < 0)
                                historyPos = 0;
                            
                            string historyString = commandHistory[(commandHistory.Length - 1) - historyPos];
                            byte[] bytes = new byte[historyString.Length];

                            for (int i = 0; i < historyString.Length; i++)
                            {
                                data->Buf[i] = (byte)historyString[i];
                            }
                            data->Buf[historyString.Length] = 0;
                            data->BufDirty = 1;
                            data->BufTextLen = historyString.Length;
                            data->CursorPos = historyString.Length;
                        }
                    }
                    break;
            }
            return 0;
        }

        public static void LogLine(string line)
        {
            if (lines.Length == MAX_LINES)
                lines.RemoveAt(0);
            lines.Add(line);

            lastRunLines++;
        }

        public static void LogLineAndSend(string line)
        {
            LogLine(line);
            if (Main.gameStateManager.netMode == GameStates.GameStateManager.NetworkingMode.Server)
            {
                SyncConsoleOutput.Instance.SendOutput([line]);
            }
        }

        [Conditional("DEBUG")]
        public static void Assert(bool condition)
        {
            if (!condition)
            {
                StackTrace trace = new StackTrace(1);
                LogLine(string.Format("Assert failed: {0}", trace.ToString()));
                throw new Exception(string.Format("Assert failed: {0}", trace.ToString()));
            }
        }

        [Conditional("DEBUG")]
        public static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                StackTrace trace = new StackTrace(1);
                LogLine(string.Format("Assert failed: {0}\n{1}", message, trace.ToString()));
                throw new Exception(string.Format("Assert failed: {0}", trace.ToString()));
            }
        }

        [Conditional("DEBUG")]
        public static void Assert(bool condition, string? message, string detailedMessage, params object[] args)
        {
            if (!condition)
            {
                StackTrace trace = new StackTrace(1);
                LogLine(string.Format("Assert failed: {0}", trace.ToString()));
                throw new Exception(string.Format("Assert failed: {0}\n{1}\n{2}", message != null ? message : "", string.Format(detailedMessage, args), trace.ToString()));
            }
        }
    }
}
