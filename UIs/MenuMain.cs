using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.GameStates;

namespace ViMG.UIs
{
    public class MenuMain : Menu
    {
        private enum MenuState 
        { 
            Main,
            Worlds,
            Settings
        }

        private MenuState state;
        private WorldSaver saver;

        private string[] directories;

        private bool clicked = false;

        private TextHelper.FontInfo fi;

        //TODO remove
        private UI.LabelConstructionParameters[] options;
        private int currentOption;
        private bool open;
        private UI.Button[] outputs;

        public MenuMain(GameStateManager gsManager) : base(gsManager)
        {
            fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
            Main.MouseControl = true;
            Main.DrawCursor = true;

            options = new UI.LabelConstructionParameters[4]
            {
                new UI.LabelConstructionParameters("Test 0", fi, 128, Vector2.Zero),
                new UI.LabelConstructionParameters("Test 1", fi, 128, Vector2.Zero),
                new UI.LabelConstructionParameters("Test 2", fi, 128, Vector2.Zero),
                new UI.LabelConstructionParameters("Test 3", fi, 128, Vector2.Zero),
            };

            outputs = new UI.Button[4];
        }

        public override void OnOpen()
        {
            base.OnOpen();

            Main.MouseControl = true;
            Main.DrawCursor = true;
        }

        public override void OnClose()
        {
            base.OnClose();

            Main.MouseControl = false;
            Main.DrawCursor = false;
        }

        public override void Update(GraphicsDevice device, double deltaTime)
        {
            UI.Start();

            UI.StartParent(new Vector2(Options.CurrentWindowResolution.X / 2 - 64, Options.CurrentWindowResolution.Y / 2 - 128));

            if (Main.inputManager.JustReleased(A1r.Input.MouseInput.LeftButton))
                clicked = false;

            if (state == MenuState.Main)
            {
                if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, 48 * 0, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"), 
                    new UI.LabelConstructionParameters("Load World", fi, 128, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                {
                    state = MenuState.Worlds;
                    directories = saver.GetWorldSaveDirectories();
                    clicked = true;
                }

                if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, 48 * 1, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new UI.LabelConstructionParameters("Continue", fi, 128, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                {
                    if (Main.SessionInformation.LastLoadedSave != null)
                    {
                        gsManager.SetGameState(gsManager.TheIsland);
                        gsManager.TheIsland.LoadWorld(Main.SessionInformation.LastLoadedSave);
                    }
                }

                if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, 48 * 2, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new UI.LabelConstructionParameters("Options", fi, 128, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                {
                    gsManager.GetCurrentGameState().PushMenu(new MenuOptions(gsManager));
                }

                if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, 48 * 3, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new UI.LabelConstructionParameters("Exit", fi, 128, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                {
                    Main.Exit = true;
                }
            }
            else if (state == MenuState.Worlds)
            {
                if (!clicked)
                {
                    if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(-32, 256, 32, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                       new UI.LabelConstructionParameters("<", fi, 32, Vector2.Zero),
                       new RectangleF(0, 64, 32, 32), new RectangleF(32, 64, 32, 32), new RectangleF(32, 64, 32, 32))).clickLeft)
                    {
                        state = MenuState.Main;
                    }

                    for (int i = 0; i < directories.Length; i++)
                    {
                        int ypos = 48 * i;

                        if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, ypos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                            new UI.LabelConstructionParameters("Load " + directories[i], fi, 128, Vector2.Zero),
                            new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                        {
                            gsManager.SetGameState(gsManager.TheIsland);
                            gsManager.TheIsland.LoadWorld(directories[i]);
                        }
                    }

                    int fypos = 48 * directories.Length;

                    if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, fypos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                        new UI.LabelConstructionParameters("Create New", fi, 128, Vector2.Zero),
                        new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                    {
                        gsManager.SetGameState(gsManager.TheIsland);
                        gsManager.TheIsland.LoadWorld("new" + directories.Length);
                    }
                }
            }

            UI.EndParent();
        }

        public override void Draw(SpriteBatch batch)
        {
            UI.Draw(batch, 1);
        }
    }
}
