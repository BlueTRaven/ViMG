using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.GameStates;
using ViMG.IMGUIImpl;
using ViMG.UIs;

namespace Engine
{
    public class HeadlessRunner
    {
        private Runner runner;

        public void Run()
        {
            GlobalState.IsHeadless = true;

            this.runner = new Runner();
            var _services = new GameServiceContainer();
            var _content = new ContentManager(_services);
            _content.RootDirectory = "Content";

            var outWriter = IMGUIConsole.ReplaceOut();

            runner.Initialize(_content);

            runner.LoadContent();

            runner.Register(null);

            GlobalState.GameStateManager.netMode = GameStateManager.NetworkingMode.Server;

            while (!GlobalState.Exit)
            {
                Console.WriteLine("Enter a Save Name or * to list available saves:");
                var saveName = Console.ReadLine();

                if (saveName == "*")
                {
                    var saveNames = MenuMain.GetWorldSaveDirectories();
                    foreach (string name in saveNames)
                    {
                        Console.WriteLine(name);
                    }

                    saveName = null;
                }
                else if (saveName == ">")
                {
                    saveName = GlobalState.SessionInformation.LastLoadedSave;
                }

                if (saveName != null)
                {
                    if (!MenuMain.GetWorldSaveDirectories().Contains(saveName))
                    {
                        Console.WriteLine("No save with this name exists. Create a new one?");
                        var answer = Console.ReadLine();
                        if (!(answer.ToLower() == "y" || answer.ToLower() == "yes"))
                        {
                            // return to top, select a new file again
                            break;
                        }
                    }
                    int port = 9050;
                    while (!GlobalState.Exit)
                    {
                        Console.WriteLine("Enter port (or press enter for the default port, {0})", port);
                        var portStr = Console.ReadLine();
                        if (portStr == "")
                            break;
                        if (int.TryParse(portStr, out port))
                            break;
                    }

                    GlobalState.GameStateManager.SetGameState(GlobalState.GameStateManager.TheIsland);
                    GlobalState.GameStateManager.TheIsland.StartServer(saveName, "localhost", port);
                    break;
                }
            }

            Thread t = new Thread(new ParameterizedThreadStart(Loop));
            t.Start();

            string[] history = [];
            int currHistory = 0;
            int lastKeyPress = 0;
            StringBuilder builder = new StringBuilder();
            while (!GlobalState.Exit)
            {
                if (Console.KeyAvailable)
                {
                    bool changed = outWriter.TotalWritten != lastKeyPress;
                    lastKeyPress = outWriter.TotalWritten + 1;
                    ConsoleKeyInfo key = Console.ReadKey(changed);
                    if (key.Key == ConsoleKey.Backspace && builder.Length > 0)
                        builder.Remove(builder.Length - 1, 1);
                    else if (key.Key == ConsoleKey.UpArrow)
                    {
                        if (history == null || IMGUIConsole.GetCommandHistroy().Length != history.Length)
                        {
                            var h = IMGUIConsole.GetCommandHistroy();
                            history = h.Slice().ToArray();
                            currHistory = 0;
                        }
                        
                        builder.Clear();
                        builder.Append(history[history.Length - 1 - currHistory]);
                        currHistory += 1;

                        currHistory = int.Clamp(currHistory, 0, history.Length - 1);
                    }
                    else
                        builder.Append(key.KeyChar);

                    if (changed)
                        Console.Write("\n" + builder.ToString());

                    if (key.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine(builder.ToString());
                        rwLock.AcquireWriterLock(0);
                        commandsToRunInGameThread.Add(builder.ToString());
                        rwLock.ReleaseWriterLock();
               
                        builder.Clear();
                    }
                }
            }
        }

        private ReaderWriterLock rwLock = new ReaderWriterLock();
        private List<string> commandsToRunInGameThread = new List<string>();

        private void Loop(object? param)
        {
            DateTime prevTime = DateTime.Now;
            while (!GlobalState.Exit)
            {
                rwLock.AcquireReaderLock(0);
                if (commandsToRunInGameThread.Count > 0)
                {
                    foreach (string command in commandsToRunInGameThread)
                    {
                        var str = command.TrimEnd();
                        var splits = str.Split(' ');
                        var runOut = IMGUIConsole.RunCommand(splits[0], Networking.NetworkManager.NetworkSide.Server, splits[1..]);
                        for (int i = 0; i < runOut.output.Length; i++)
                        {
                            Console.WriteLine(runOut.output[i]);
                        }
                    }
                    rwLock.UpgradeToWriterLock(0);
                    commandsToRunInGameThread.Clear();
                }
                rwLock.ReleaseLock();

                DateTime now = DateTime.Now;
                TimeSpan delta = now - prevTime;

                int numFixedUpdates = runner.UnfixedUpdate(delta);
                for (int i = 0; i < numFixedUpdates; i++)
                {
                    runner.FixedUpdate(Main.FIXED_STEP * Options.DEBUGTimescale);
                }

                prevTime = now;
            }
        }
    }
}
