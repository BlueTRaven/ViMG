using Engine.Mods;
using Hexa.NET.ImGui;
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
using ViMG.IMGUIImpl;

namespace Engine
{
    public class Runner : IDisposable
    {
        private ModManager modManager;

        private double accumulator;

        public void Initialize(ContentManager content)
        {
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

            IMGUIConsole.CollectCommands();
        }

        public int UnfixedUpdate(TimeSpan elapsed)
        {
            GlobalState.Time += elapsed.TotalSeconds;

            GlobalState.GameStateManager.UnfixedUpdate(elapsed.TotalSeconds);

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
            GlobalState.GameStateManager.Update(deltaTime);
        }

        public void Dispose()
        {
            GlobalState.GameStateManager.Dispose();
        }
    }
}
