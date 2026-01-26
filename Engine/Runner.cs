using Engine.Mods;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.GameStates;

namespace Engine
{
    public class Runner
    {
        private ModManager modManager;

        private double accumulator;

        public void Initialize(ContentManager content)
        {
            GlobalState.MainThread = Thread.CurrentThread;

            GlobalState.SessionInformation = new SessionInformation();
            GlobalState.SessionIO = new SessionIO();
            GlobalState.SessionIO.Load();

            GlobalState.AssetsManager = new ViMGAssetsManager(content);

            GlobalState.GameStateManager = new GameStateManager();
            GlobalState.GameStateManager.Initialize();
            modManager = new ModManager();
        }

        public void LoadContent()
        {
            GlobalState.AssetsManager.LoadContent(Directory.GetCurrentDirectory() + "/Content");
        }

        public void Register(GraphicsDevice? device)
        {
            modManager.LoadModDlls();
            GlobalState.Registry = new RegistryService(device);
            GlobalState.Registry.Register();
        }

        public int UnfixedUpdate(TimeSpan elapsed)
        {
            int numUpdates = 0;
            accumulator += elapsed.TotalSeconds;
            while (accumulator >= Main.FIXED_STEP && !GlobalState.Exit)
            {
                accumulator -= Main.FIXED_STEP;

                numUpdates += 1;
            }

            return numUpdates;
        }

        public void FixedUpdate(double deltaTime)
        {
            GlobalState.Time += deltaTime;

            GlobalState.GameStateManager.Update(deltaTime);

        }
    }
}
