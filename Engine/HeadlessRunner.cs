using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
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
        public class TrackedCommand
        {
            public string command;
            public string output;
            public ManualResetEventSlim waiter;
        };

        private static Logger Logger = Logger.InitLogger("HeadlessRunner", true, Logger.LogLevel.Info);

        //[ConsoleCommandVar("updating_paused", "Pauses updating")]
        //public static bool UpdatingPaused = false;

        //[ConsoleCommandVar("paused_do_updates", "While updating_paused is true, update for the specified number of updates. Has no effect while unpaused.")]
        //public static int NumUpdates = 0;

        //[ConsoleCommandVar("paused_do_fixed_updates", "While updating_paused is true, fixed update for the specified number of updates. Has no effect while unpaused. Note that unfixed updates will occur too.")]
        //public static int NumFixedUpdates = 0;

        private ManualResetEventSlim? currentUpdateWaiter = null;
        private bool updatingPaused = false;
        private int numUpdates = 0;
        private int numFixedUpdates = 0;

        private Runner runner;

        private ReaderWriterLock rwLock = new ReaderWriterLock();
        private List<TrackedCommand> commandsToRunInGameThread = new List<TrackedCommand>();

        public void Run()
        {
            Thread.CurrentThread.Name = "Headless Runner Thread";
            ViMG.TracyImpl.Tracy.SetThreadName("Headless Runner Thread");

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
                string? saveName = GlobalState.Args.saveName;
                if (saveName == null)
                {
                    Console.WriteLine("Enter a Save Name or * to list available saves:");
                    saveName = Console.ReadLine();

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
                }

                if (saveName != null)
                {
                    if (!MenuMain.GetWorldSaveDirectories().Contains(saveName))
                    {
                        // createSave overrides these options
                        if (!GlobalState.Args.createSave)
                        {
                            Console.WriteLine("No save with this name exists. Create a new one?");
                            var answer = Console.ReadLine();
                            if (!(answer.ToLower() == "y" || answer.ToLower() == "yes"))
                            {
                                // return to top, select a new file again
                                continue;
                            }
                        }
                    }
                    int port = GlobalState.Args.defaultPort;
                    while (!GlobalState.Args.defaultPortSpecified && !GlobalState.Exit)
                    {
                        Console.WriteLine("Enter port (or press enter for the default port, {0})", port);
                        var portStr = Console.ReadLine();
                        if (portStr == "")
                            break;
                        if (int.TryParse(portStr, out port))
                            break;
                    }

                    GlobalState.GameStateManager.SetGameState(GlobalState.GameStateManager.TheIsland);
                    try
                    {
                        if (GlobalState.GameStateManager.TheIsland.StartServer(saveName, "localhost", port))
                            break;
                        else throw new Exception("Failed to start server");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        continue;
                    }
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
                        PostCommand(builder.ToString());
                        builder.Clear();
                    }
                }
            }

            Logger.Info("Shutting down HeadlessRunner Thread...");
            t.Join();
            Logger.Info("Done.");
        }

        public TrackedCommand PostCommand(string command)
        {
            TrackedCommand tracked = new TrackedCommand()
            {
                command = command,
                waiter = new ManualResetEventSlim(false),
            };

            rwLock.AcquireWriterLock(0);
            commandsToRunInGameThread.Add(tracked);
            rwLock.ReleaseWriterLock();

            return tracked;
        }

        private void Loop(object? param)
        {
            Logger.Debug("Begin loop");

            Thread.CurrentThread.Name = "Game Thread";
            ViMG.TracyImpl.Tracy.SetThreadName("Game Thread");
            DateTime prevTime = DateTime.Now;
            while (!GlobalState.Exit)
            {
                ViMG.TracyImpl.Tracy.FrameMark();

                rwLock.AcquireReaderLock(0);
                if (commandsToRunInGameThread.Count > 0)
                {
                    foreach (TrackedCommand command in commandsToRunInGameThread)
                    {
                        var str = command.command.TrimEnd();
                        var splits = str.Split(' ');
                        var runOut = IMGUIConsole.RunCommand(splits[0], Networking.NetworkManager.NetworkSide.Server, splits[1..]);
                        for (int i = 0; i < runOut.output.Length; i++)
                        {
                            Console.WriteLine(runOut.output[i]);
                            command.output = string.Join('\n', runOut.output);
                        }
                        command.waiter.Set();
                    }
                    rwLock.UpgradeToWriterLock(0);
                    commandsToRunInGameThread.Clear();
                }
                rwLock.ReleaseLock();

                DateTime now = DateTime.Now;
                TimeSpan delta = now - prevTime;

                if (GlobalState.GameStateManager.TheIsland.PollWorldLoaded())
                {
                    if (!updatingPaused || (numUpdates > 0 || numFixedUpdates > 0))
                    {
                        var oldPaused = false;
                        if (numUpdates > 0 || numFixedUpdates > 0)
                        {
                            oldPaused = GlobalState.GameStateManager.Paused;
                            GlobalState.GameStateManager.Paused = false;
                        }

                        int unfixedUpdatesInThisTimestep = runner.UnfixedUpdate(delta);
                        for (int i = 0; i < unfixedUpdatesInThisTimestep; i++)
                        {
                            runner.FixedUpdate(Main.FIXED_STEP * Options.DEBUGTimescale);
                            if (numFixedUpdates > 0)
                                numFixedUpdates -= 1;

                            if (numFixedUpdates <= 0)
                            {
                                Logger.Info("Ran {0} fixed upates as requested", numFixedUpdates);
                                currentUpdateWaiter?.Set();
                                currentUpdateWaiter = null;
                            }
                        }

                        if (numUpdates > 0 || numFixedUpdates > 0)
                            GlobalState.GameStateManager.Paused = oldPaused;

                        if (numUpdates > 0)
                            numUpdates -= 1;
                        if (numUpdates <= 0)
                        {
                            Logger.Info("Ran {0} upates as requested", numFixedUpdates);
                            currentUpdateWaiter?.Set();
                            currentUpdateWaiter = null;
                        }
                    }
                }

                prevTime = now;
            }

            Logger.Info("Shutting down Game Thread...");
            runner.Dispose();
            Logger.Info("Done.");
        }

        // Returns an event that can be waited upon; when the number of updates is completed, the event is set.
        public ManualResetEventSlim UpdateNTimes(int numTimes)
        {
            currentUpdateWaiter = new ManualResetEventSlim(false);
            updatingPaused = true;
            numUpdates = numTimes;
            return currentUpdateWaiter;
        }
        
        // Returns an event that can be waited upon; when the number of updates is completed, the event is set.
        public ManualResetEventSlim FixedUpdateNTimes(int numTimes)
        {
            currentUpdateWaiter = new ManualResetEventSlim(false);
            updatingPaused = true;
            numFixedUpdates = numTimes;
            return currentUpdateWaiter;
        }

        // Thread safe. Pauses updating on the game thread.
        public void PauseUpdating()
        {
            Debug.Assert(!updatingPaused);
            updatingPaused = true;
        }

        // Thread safe. Resumes updating on the game thread.
        public void ResumeUpdating()
        {
            Debug.Assert(updatingPaused);
            updatingPaused = false;
        }
    }
}
