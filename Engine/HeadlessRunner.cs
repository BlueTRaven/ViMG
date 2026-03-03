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
    public class HeadlessRunner : IDisposable
    {
        public class Output
        {
            public Exception? runnerThreadException;
            public Exception? gameThreadException;
        }

        public class TrackedCommand
        {
            public required string command;
            public string? output;
            public required ManualResetEventSlim waiter;
        };

        public delegate object? GameThreadFn(HeadlessRunner runner, object? addData);
        public class TrackedGameThreadFn
        {
            public required GameThreadFn func;
            public object? addData;
            public object? output;
            public required ManualResetEventSlim waiter;
        }

        private static Logger Logger = Logger.InitLogger("HeadlessRunner", true, Logger.LogLevel.Info);

        private ManualResetEventSlim? currentUpdateWaiter = null;
        private ManualResetEventSlim? currentFixedUpdateWaiter = null;
        private bool updatingPaused = false;
        private int numUpdatesCurrent = 0;
        private int numUpdates = 0;
        private int numFixedUpdatesCurrent = 0;
        private int numFixedUpdates = 0;

        private ManualResetEventSlim waiterGameStateInit = new ManualResetEventSlim();

        private readonly Runner runner;

        private readonly ReaderWriterLock commandsLock = new ReaderWriterLock();
        private readonly List<TrackedCommand> commandsToRunInGameThread = new List<TrackedCommand>();
        private readonly ReaderWriterLock gameFnsLock = new ReaderWriterLock();
        private readonly List<TrackedGameThreadFn> funcsToRunInGameThread = new List<TrackedGameThreadFn>();

        private readonly IMGUIConsole.ConsoleTextWriter outWriter;

        public readonly CancellationTokenSource cts;
        private readonly CancellationToken ct;

        private Thread loopThread;

        public Output output = new();

        public HeadlessRunner(CancellationToken ct) : this()
        {
            this.ct = ct;
        }

        public HeadlessRunner()
        {
            cts = new CancellationTokenSource();
            GlobalState.IsHeadless = true;
            GlobalState.Exit = false;

            runner = new Runner();
            var _services = new GameServiceContainer();
            var _content = new ContentManager(_services);
            _content.RootDirectory = "Content";

            outWriter = IMGUIConsole.ReplaceOut();

            runner.Initialize(_content);

            runner.LoadContent();

            runner.Register(null);

            GlobalState.GameStateManager.netMode = GameStateManager.NetworkingMode.Server;

            loopThread = new Thread(ThreadFunc);

            waiterGameStateInit.Set();
        }

        public void Run()
        {
            try
            {
                DoRun();
            }
            catch (Exception e)
            {
                output.runnerThreadException = e;
                cts.Cancel();
            }
        }

        private void DoRun()
        {
            GlobalState.MainThread = Thread.CurrentThread;

            Thread.CurrentThread.Name = "Headless Runner Thread";
            ViMG.TracyImpl.Tracy.SetThreadName("Headless Runner Thread");

            GlobalState.GameStateManager.TheIsland.Reset();

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

            loopThread.Start(output);

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
            loopThread.Join();
            Logger.Info("Done.");
        }

        private void ThreadFunc(object? param)
        {
            Output output = param as Output;
            try
            {
                Loop();
            }
            catch (Exception e)
            {
                output.gameThreadException = e;
                cts.Cancel();
            }
        }

        private void Loop()
        {
            Logger.Debug("Begin loop");

            Thread.CurrentThread.Name = "Game Thread";
            ViMG.TracyImpl.Tracy.SetThreadName("Game Thread");
            DateTime prevTime = DateTime.Now;
            while (!GlobalState.Exit)
            {
                ViMG.TracyImpl.Tracy.FrameMark();

                commandsLock.AcquireReaderLock(0);
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
                    commandsLock.UpgradeToWriterLock(0);
                    commandsToRunInGameThread.Clear();
                }
                commandsLock.ReleaseLock();

                gameFnsLock.AcquireReaderLock(0);
                if (funcsToRunInGameThread.Count > 0)
                {
                    foreach (TrackedGameThreadFn func in funcsToRunInGameThread)
                    {
                        object? output = func.func(this, func.addData);
                        func.output = output;
                        func.waiter.Set();
                    }
                    gameFnsLock.UpgradeToWriterLock(0);
                    funcsToRunInGameThread.Clear();
                }
                gameFnsLock.ReleaseLock();

                DateTime now = DateTime.Now;
                TimeSpan delta = now - prevTime;

                if (GlobalState.GameStateManager.TheIsland.NetPoll())
                {
                    if (!updatingPaused || (numUpdatesCurrent > 0 || numFixedUpdatesCurrent > 0))
                    {
                        var oldPaused = false;
                        if (numUpdatesCurrent > 0 || numFixedUpdatesCurrent > 0)
                        {
                            oldPaused = GlobalState.GameStateManager.Paused;
                            GlobalState.GameStateManager.Paused = false;
                        }

                        int unfixedUpdatesInThisTimestep = runner.UnfixedUpdate(delta);
                        for (int i = 0; i < unfixedUpdatesInThisTimestep; i++)
                        {
                            runner.FixedUpdate(Main.FIXED_STEP * Options.DEBUGTimescale);
                            if (numFixedUpdatesCurrent > 0)
                                numFixedUpdatesCurrent -= 1;
                            if (currentFixedUpdateWaiter != null && numFixedUpdatesCurrent <= 0)
                            {
                                Logger.Info("Ran {0} fixed upates as requested", numFixedUpdates);
                                currentFixedUpdateWaiter?.Set();
                                currentFixedUpdateWaiter = null;
                            }
                        }

                        if (numUpdatesCurrent > 0 || numFixedUpdatesCurrent > 0)
                            GlobalState.GameStateManager.Paused = oldPaused;

                        if (numUpdatesCurrent > 0)
                            numUpdatesCurrent -= 1;
                        if (currentUpdateWaiter != null && numUpdatesCurrent <= 0)
                        {
                            Logger.Info("Ran {0} upates as requested", numUpdates);
                            currentUpdateWaiter?.Set();
                            currentUpdateWaiter = null;
                        }
                    }
                }

                prevTime = now;
            }

            Logger.Info("Shutting down Game Thread...");
            Logger.Info("Done.");
        }

        // Returns an event that can be waited upon; when the number of updates is completed, the event is set.
        public ManualResetEventSlim UpdateNTimes(int numTimes)
        {
            currentUpdateWaiter = new ManualResetEventSlim(false);
            updatingPaused = true;
            numUpdates = numTimes;
            numUpdatesCurrent = numTimes;
            return currentUpdateWaiter;
        }
        
        // Returns an event that can be waited upon; when the number of updates is completed, the event is set.
        public ManualResetEventSlim FixedUpdateNTimes(int numTimes)
        {
            currentFixedUpdateWaiter = new ManualResetEventSlim(false);
            updatingPaused = true;
            numFixedUpdates = numTimes;
            numFixedUpdatesCurrent = numTimes;
            return currentFixedUpdateWaiter;
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

        public TrackedCommand PostCommand(string command)
        {
            TrackedCommand tracked = new TrackedCommand()
            {
                command = command,
                waiter = new ManualResetEventSlim(false),
            };

            commandsLock.AcquireWriterLock(0);
            commandsToRunInGameThread.Add(tracked);
            commandsLock.ReleaseWriterLock();

            return tracked;
        }

        public TrackedCommand PostCommandAndWait(CancellationToken ct, string command)
        {
            var tracked = PostCommand(command);
            tracked.waiter.Wait(ct);
            if (ct.IsCancellationRequested)
            {
                if (output.runnerThreadException != null) throw output.runnerThreadException;
                if (output.gameThreadException != null) throw output.gameThreadException;
            }
            return tracked;
        }

        public TrackedGameThreadFn PostFn(GameThreadFn func, object? addData = null)
        {
            TrackedGameThreadFn tracked = new()
            {
                func = func,
                addData = addData,
                waiter = new ManualResetEventSlim(false),
            };

            gameFnsLock.AcquireWriterLock(0);
            funcsToRunInGameThread.Add(tracked);
            gameFnsLock.ReleaseWriterLock();
            return tracked;
        }

        public TrackedGameThreadFn PostFnAndWait(CancellationToken ct, GameThreadFn func, object? addData = null)
        {
            var tracked = PostFn(func, addData);
            tracked.waiter.Wait(ct);
            if (ct.IsCancellationRequested)
            {
                if (output.runnerThreadException != null) throw output.runnerThreadException;
                if (output.gameThreadException != null) throw output.gameThreadException;
            }
            return tracked;
        }

        public void WaitUntilGameStateInit(CancellationToken ct)
        {
            waiterGameStateInit.Wait(ct);
            Debug.Assert(GlobalState.GameStateManager != null);
        }

        public void WaitUntilWorldLoaded(CancellationToken ct)
        {
            waiterGameStateInit.Wait(ct);
            Debug.Assert(GlobalState.GameStateManager != null);
            Debug.Assert(GlobalState.GameStateManager.TheIsland != null);
            GlobalState.GameStateManager.TheIsland.waiterWorld.Wait(ct);
        }

        public void Dispose()
        {
            GlobalState.Exit = true;
            cts.Cancel();

            loopThread?.Join();
            runner.Dispose();
            
            if (output.runnerThreadException != null) throw output.runnerThreadException;
            if (output.gameThreadException != null) throw output.gameThreadException;
        }
    }
}
