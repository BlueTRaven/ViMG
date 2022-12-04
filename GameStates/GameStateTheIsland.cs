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
        private readonly GraphicsDevice device;

        private Task<World> worldTask;
        private World world;

        public GameStateTheIsland(GameStateManager manager, GraphicsDevice device) : base(manager)
        {
            this.device = device;
        }

        public void LoadWorld(string folderName)
        {
            worldTask = new Task<World>(() =>
            {
                World world = new World(manager, device, 512);
                world.LoadWorld(device, folderName);

                return world;
            });

            worldTask.Start();
        }

        public override void OnOpen(GameState changingFrom)
        {
            base.OnOpen(changingFrom);
        }

        public override void OnClose(GameState changingTo)
        {
            base.OnClose(changingTo);

            if (world != null)
            {
                world.ChunkLoadManager.UnloadAll();
                world.ChunkLoadManager.Dispose();
                world.ChunkManager2.Dispose();
                world = null;
            }
            SetMenu(null);
        }

        public override void Update(GraphicsDevice device, double deltaTime)
        {
            if (world == null)
            {
                if (worldTask.Wait(1))
                {
                    world = worldTask.Result;
                    world.Sync(device);
                }
            }

            if (world != null && !manager.Paused)
            {
                world.Update(deltaTime);
            }

            base.Update(device, deltaTime);
        }

        public override void Draw(GraphicsDevice device)
        {
            base.Draw(device);

            if (world != null)
            {
                world.Draw(device, null);
            }
        }

        public override void DrawUI(SpriteBatch batch)
        {
            base.DrawUI(batch);

            if (world != null)
            {
                world.DrawUI(batch);
            }
        }
    }
}
