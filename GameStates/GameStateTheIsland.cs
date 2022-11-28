using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.GameStates
{
    public class GameStateTheIsland : GameState
    {
        public World World;
        private readonly GraphicsDevice device;

        public GameStateTheIsland(GameStateManager manager, GraphicsDevice device) : base(manager)
        {
            this.device = device;
        }

        public void LoadWorld(string folderName)
        {
            World.LoadWorld(device, folderName);
        }

        public override void OnOpen(GameState changingFrom)
        {
            base.OnOpen(changingFrom);

            World = new World(manager, device, 512);
        }

        public override void OnClose(GameState changingTo)
        {
            base.OnClose(changingTo);

            World.ChunkLoadManager.UnloadAll();
            World.ChunkLoadManager.Dispose();
            World.ChunkManager2.Dispose();
            World = null;
            SetMenu(null);
        }

        public override void Update(GraphicsDevice device, double deltaTime)
        {
            if (World.LoadedFolderName != null && !manager.Paused)
            {
                World.Update(deltaTime);
            }

            base.Update(device, deltaTime);
        }

        public override void Draw(GraphicsDevice device)
        {
            base.Draw(device);

            if (World.LoadedFolderName != null)
            {
                World.Draw(device, null);
            }
        }

        public override void DrawUI(SpriteBatch batch)
        {
            base.DrawUI(batch);

            if (World.LoadedFolderName != null)
            {
                World.DrawUI(batch);
            }
        }
    }
}
