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
    public class Runner
    {
        public void Initialize(ContentManager content)
        {
            GlobalState.MainThread = Thread.CurrentThread;

            GlobalState.SessionInformation = new SessionInformation();
            GlobalState.SessionIO = new SessionIO();
            GlobalState.SessionIO.Load();

            GlobalState.AssetsManager = new ViMGAssetsManager(content);

            GlobalState.GameStateManager = new GameStateManager();
            GlobalState.GameStateManager.Initialize();
        }

        public void LoadContent()
        {
            GlobalState.AssetsManager.LoadContent(Directory.GetCurrentDirectory() + "/Content");
        }
    }
}
