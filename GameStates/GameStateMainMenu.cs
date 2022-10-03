using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.UIs;

namespace ViMG.GameStates
{
    public class GameStateMainMenu : GameState
    {
        private MenuMain menuMain;

        private World world;

        private GraphicsDevice device;

        public GameStateMainMenu(GameStateManager manager, GraphicsDevice device) : base(manager)
        {
            this.device = device;
        }

        public override void Initialize(GraphicsDevice device)
        {
            base.Initialize(device);
        }

        public override void OnClose(GameState changingTo)
        {
            base.OnClose(changingTo);

            if (changingTo is GameStateTheIsland island)
                island.World = world;
        }

        public override void OnOpen(GameState changingFrom)
        {
            world = new World(manager, device, 512);

            menuMain = new MenuMain(manager, world);
            SetMenu(menuMain);

            base.OnOpen(changingFrom);
        }

        public override void Update(GraphicsDevice device, double deltaTime)
        {
            base.Update(device, deltaTime);

            if (world.LoadedFolderName != null)
            {
                world.Update(deltaTime);
            }
        }

        public override void Draw(GraphicsDevice device)
        {
            base.Draw(device);

            if (world.LoadedFolderName != null)
            {
                world.Draw(device, null);
            }
        }

        public override void DrawUI(SpriteBatch batch)
        {
            base.DrawUI(batch);

            if (world.LoadedFolderName != null)
            {
                world.DrawUI(batch);
            }
        }
    }
}
