using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.UIs;

namespace ViMG.GameStates
{
    public class GameStateTheIsland : GameState
    {
        private readonly GraphicsDevice device;
        private readonly TextHelper.FontInfo fi;
        private Task<World> worldTask;
        private World world;

        public bool IsLoading;
        private string loadMessage;
        public string LoadMessage 
        {
            get 
            {
                object toLock = loadMessage == null ? device : loadMessage;
                lock (toLock) 
                {
                    //return a COPY since we might be modifying this value.
                    //This is slow, but whatever, we're only using this during loading.
                    return new string(loadMessage); 
                } 
            }
            set 
            {
                object toLock = loadMessage == null ? device : loadMessage;
                lock (toLock) 
                {
                    loadMessage = value; 
                }
            }
        }

        public GameStateTheIsland(GameStateManager manager, GraphicsDevice device) : base(manager)
        {
            this.device = device;

            fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
        }

        public void LoadWorld(string folderName)
        {
            IsLoading = true;
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

                    IsLoading = false;
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

            if (IsLoading && LoadMessage != null)
            {
                string loadMessage = LoadMessage;

                TextHelper.DrawText(batch, fi, loadMessage, Color.White, new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y), Enums.Alignment.Center, Options.CurrentWindowResolution.X, 1);
            }
        }
    }
}
