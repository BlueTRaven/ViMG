using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.GameStates;
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

            runner.Initialize(_content);

            runner.LoadContent();

            runner.Register(null);

            GlobalState.GameStateManager.netMode = GameStateManager.NetworkingMode.Server;

            while (true)
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
                    while (true)
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
                }
            }

            DateTime prevTime = DateTime.Now;
            while (true)
            {
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
