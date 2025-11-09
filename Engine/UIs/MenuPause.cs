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

        private readonly World world;

        public MenuPause(GameStateManager gsManager, World world) : base(gsManager)
        {
            fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);

            this.world = world;
        }

        public override void OnOpen()
        {
            base.OnOpen();

            gsManager.Paused = true;
            Main.MouseControl = true;
            Main.DrawCursor = true;
        }

        public override void OnClose()
        {
            base.OnClose();

            gsManager.Paused = false;
            Main.MouseControl = false;
            Main.DrawCursor = false;
        }

        public override void Update(GraphicsDevice device, double deltaTime)
        {
            base.Update(device, deltaTime);

            UI.Start();

            UI.StartParent(new Vector2(Options.CurrentWindowResolution.X / 2 - 64, Options.CurrentWindowResolution.Y / 2 - 128));

            int y = 0;

            if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, y, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new UI.LabelConstructionParameters("Resume", fi, 128, Vector2.Zero),
                new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft ||
                Main.inputManager.JustPressed(Keys.Escape))
            {
                //return to old menu.
                gsManager.GetCurrentGameState().PopMenu();
            }

            y += 32 + MARGIN;

            if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, y, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                new UI.LabelConstructionParameters("Save", fi, 128, Vector2.Zero),
                new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
            {
                world.SaveWorld();
            }

            y += 32 + MARGIN;

            if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, y, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                new UI.LabelConstructionParameters("Options", fi, 128, Vector2.Zero),
                new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
            {
                gsManager.GetCurrentGameState().PushMenu(new MenuOptions(gsManager));
            }

            y += 32 + MARGIN;

            if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, y, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                new UI.LabelConstructionParameters("Exit To Title", fi, 128, Vector2.Zero),
                new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
            {
                world.GameStateManager.SetGameState(world.GameStateManager.MainMenu);
            }

            y += 32 + MARGIN;

            if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, y, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                new UI.LabelConstructionParameters("Exit To Desktop", fi, 128, Vector2.Zero),
                new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
            {
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
