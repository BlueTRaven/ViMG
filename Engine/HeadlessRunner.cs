using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.GameStates;

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
            var saveName = Console.ReadLine();

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
