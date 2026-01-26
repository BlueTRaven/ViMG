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
        public void Run()
        {
            GlobalState.MainThread = Thread.CurrentThread;

            GlobalState.SessionInformation = new SessionInformation();
            GlobalState.SessionIO = new SessionIO();
            GlobalState.SessionIO.Load();

            var _services = new GameServiceContainer();
            var _content = new ContentManager(_services);

            GlobalState.AssetsManager = new ViMGAssetsManager(_content);

            //gameStateManager = new GameStateManager();
            //gameStateManager.Initialize();
        }
    }
}
