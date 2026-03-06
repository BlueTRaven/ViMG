using BrUtility;
using Engine;
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
        private Texture2D uiTex;
        private UI.ButtonConstructionParameters buttonParams;

        public MenuPause(GameStateManager gsManager) : base(gsManager)
        {
            fi = new TextHelper.FontInfo(GlobalState.AssetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
            uiTex = GlobalState.AssetsManager.GetAsset<Texture2D>("ui_buttons");

            buttonParams = new UI.ButtonConstructionParameters
            {
                bounds = new RectangleF(0, 0, 128, 32),
                label = new UI.LabelConstructionParameters("", fi, 128, Vector2.Zero, alignment: Enums.Alignment.Center, height: 32),
                nsSource = new BrNineSlice.NineSlice(uiTex, new RectangleF(0, 0, 128, 32), 4, 4, 4, 4),
                nsClicked = new BrNineSlice.NineSlice(uiTex, new RectangleF(0, 32, 128, 32), 4, 4, 4, 4),
                nsHovered = new BrNineSlice.NineSlice(uiTex, new RectangleF(0, 32, 128, 32), 4, 4, 4, 4),
                color = Color.White,
            };
        }

        public override void OnOpen()
        {
            base.OnOpen();

            if (GlobalState.NetMode == NetworkingMode.Singleplayer)
                gsManager.Paused = true;
            Main.MouseControl = true;
            Main.DrawCursor = true;
        }

        public override void OnClose()
        {
            base.OnClose();

            if (GlobalState.NetMode == NetworkingMode.Singleplayer)
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

            if (UI.MakeButton(buttonParams with
            {
                label = buttonParams.label.WithNewText("Resume"),
                bounds = buttonParams.bounds with { y = y },
            }).clickLeft ||
                (Main.inputManager.JustPressed(Keys.Escape) && RespondToInput))
            {
                //return to old menu.
                gsManager.GetCurrentGameState().PopMenu();
            }

            y += 32 + MARGIN;

            if (GlobalState.NetMode != NetworkingMode.Client)
            {
                if (UI.MakeButton(buttonParams with 
                {
                    label = buttonParams.label.WithNewText("Save"),
                    bounds = buttonParams.bounds with { y = y },
                }).clickLeft)
                {
                    gsManager.TheIsland.Save(false);
                }

                y += 32 + MARGIN;
            }

            if (UI.MakeButton(buttonParams with
            {
                label = buttonParams.label.WithNewText("Options"),
                bounds = buttonParams.bounds with { y = y },
            }).clickLeft)
            {
                gsManager.GetCurrentGameState().PushMenu(new MenuOptions(gsManager));
            }

            y += 32 + MARGIN;

            if (UI.MakeButton(buttonParams with
            {
                label = buttonParams.label.WithNewText("Exit to\nMenu") with
                {
                    height = buttonParams.bounds.height + 16,
                },
                bounds = buttonParams.bounds with { y = y, height = buttonParams.bounds.height + 16 },
            }).clickLeft)
            {
                GlobalState.GameStateManager.SetGameState(GlobalState.GameStateManager.MainMenu);
            }

            y += 48 + MARGIN;

            if (UI.MakeButton(buttonParams with
            {
                label = buttonParams.label.WithNewText("Exit to\nDesktop") with
                {
                    height = buttonParams.bounds.height + 16,
                },
                bounds = buttonParams.bounds with { y = y, height = buttonParams.bounds.height + 16 },
            }).clickLeft)
            {
                GlobalState.GameStateManager.TheIsland.Disconnect();
                GlobalState.Exit = true;
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
