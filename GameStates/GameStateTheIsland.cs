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

        public GameStateTheIsland(GameStateManager manager) : base(manager)
        {
        }

        public override void OnClose(GameState changingTo)
        {
            base.OnClose(changingTo);

            SetMenu(null);
        }

        public override void Update(GraphicsDevice device, double deltaTime)
        {
            base.Update(device, deltaTime);

            if (World.LoadedFolderName != null && !manager.Paused)
            {
                World.Update(deltaTime);
            }
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
