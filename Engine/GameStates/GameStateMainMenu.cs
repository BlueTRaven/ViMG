using Engine;
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

        public GameStateMainMenu(GameStateManager manager) : base(manager)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void LoadContent(GraphicsDevice device)
        {
            base.LoadContent(device);

            if (!GlobalState.IsHeadless)
                menuMain.LoadContent();
        }

        public override void OnOpen(GameState changingFrom)
        {
            if (!GlobalState.IsHeadless)
            {
                menuMain = new MenuMain(manager);
                menuMain.LoadContent();
                SetMenu(menuMain);
            }

            base.OnOpen(changingFrom);
        }

        public override void Update(double deltaTime)
        {
            /*if (world.LoadedFolderName != null)
            {
                world.Update(deltaTime);
            }*/

            base.Update(deltaTime);
        }

        public override void Draw(GraphicsDevice device, SpriteBatch batch, double deltaTime)
        {
            base.Draw(device, batch, deltaTime);

            /*if (world.LoadedFolderName != null)
            {
                world.Draw(device, null);
            }*/
        }

        public override void DrawUI(SpriteBatch batch)
        {
            base.DrawUI(batch);

            /*if (world.LoadedFolderName != null)
            {
                world.DrawUI(batch);
            }*/
        }
    }
}
