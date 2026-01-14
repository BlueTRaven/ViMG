using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.GameStates;

namespace ViMG.UIs
{
    public class MenuPause : Menu
    {
        private TextHelper.FontInfo fi;

        public MenuPause(GameStateManager gsManager) : base(gsManager)
        {
            fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
        }

        public override void OnOpen()
        {
            base.OnOpen();

            if (Main.gameStateManager.netMode == GameStateManager.NetworkingMode.Singleplayer)
                gsManager.Paused = true;
            Main.MouseControl = true;
            Main.DrawCursor = true;
        }

        public override void OnClose()
        {
            base.OnClose();

            if (Main.gameStateManager.netMode == GameStateManager.NetworkingMode.Singleplayer)
                gsManager.Paused = false;
            Main.MouseControl = false;
            Main.DrawCursor = false;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            UI.Start();

            UI.StartParent(new Vector2(Options.CurrentWindowResolution.X / 2 - 64, Options.CurrentWindowResolution.Y / 2 - 128));

            int y = 0;

            if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, y, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new UI.LabelConstructionParameters("Resume", fi, 128, Vector2.Zero),
                new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft ||
                (Main.inputManager.JustPressed(Keys.Escape) && RespondToInput))
            {
                //return to old menu.
                gsManager.GetCurrentGameState().PopMenu();
            }

            y += 32 + MARGIN;

            if (gsManager.netMode != GameStateManager.NetworkingMode.Client)
            {
                if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, y, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new UI.LabelConstructionParameters("Save", fi, 128, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                {
                    gsManager.TheIsland.Save(false);
                }

                y += 32 + MARGIN;
            }

            if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, y, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                new UI.LabelConstructionParameters("Options", fi, 128, Vector2.Zero),
                new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
            {
                gsManager.GetCurrentGameState().PushMenu(new MenuOptions(gsManager));
            }

            y += 32 + MARGIN;

            if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, y, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                new UI.LabelConstructionParameters("Exit to Title", fi, 128, Vector2.Zero),
                new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
            {
                Main.gameStateManager.SetGameState(Main.gameStateManager.MainMenu);
            }

            y += 32 + MARGIN;

            if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, y, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                new UI.LabelConstructionParameters("Exit to Desktop", fi, 128, Vector2.Zero),
                new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
            {
                Main.gameStateManager.TheIsland.Disconnect();
                Main.Exit = true;
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);

            UI.Draw(batch, 1);

            batch.DrawRectangle(new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y), Color.Black * 0.5f);
        }
    }
}
