using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.UIs
{
    public class UIMainMenu
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

        bool clicked = false;

        TextHelper.FontInfo fi;

        public UIMainMenu()
        {
            fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
            Main.MouseControl = true;
            Main.DrawCursor = true;

            saver = new WorldSaver(null, null);
        }

        public void Update(World world)
        {
            UI.Start();

            UI.StartParent(new Vector2(Main.WindowResolution.X / 2 - 64, Main.WindowResolution.Y / 2 - 128));

            if (Main.inputManager.JustReleased(A1r.Input.MouseInput.LeftButton))
                clicked = false;

            if (state == MenuState.Main)
            {
                if (UI.MakeButton(new RectangleF(0, 0, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"), 
                    UI.MakeLabel("Load World", fi, 128, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32)).clickLeft)
                {
                    state = MenuState.Worlds;
                    directories = saver.GetSaveDirectories();
                    clicked = true;
                }

                if (UI.MakeButton(new RectangleF(0, 48, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"), 
                    UI.MakeLabel("Continue", fi, 128, new Vector2(0, 48)),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32)).clickLeft)
                {

                }

                if (UI.MakeButton(new RectangleF(0, 96, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"), 
                    UI.MakeLabel("Exit", fi, 128, new Vector2(0, 96)),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32)).clickLeft)
                {
                    Main.Exit = true;
                }
            }
            else if (state == MenuState.Worlds)
            {
                if (!clicked)
                {
                    if (UI.MakeButton(new RectangleF(-32, 256, 32, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                       UI.MakeLabel("<", fi, 32, new Vector2(-32, 256)),
                       new RectangleF(0, 64, 32, 32), new RectangleF(32, 64, 32, 32), new RectangleF(32, 64, 32, 32)).clickLeft)
                    {
                        state = MenuState.Main;
                    }

                    for (int i = 0; i < directories.Length; i++)
                    {
                        int ypos = 48 * i;

                        if (UI.MakeButton(new RectangleF(0, ypos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                            UI.MakeLabel("Load " + directories[i], fi, 128, new Vector2(0, ypos)),
                            new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32)).clickLeft)
                        {
                            world.LoadWorld(directories[i]);

                            Main.MouseControl = false;
                            Main.DrawCursor = false;
                        }
                    }

                    int fypos = 48 * directories.Length;

                    if (UI.MakeButton(new RectangleF(0, fypos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                        UI.MakeLabel("Create New", fi, 128, new Vector2(0, fypos)),
                        new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32)).clickLeft)
                    {
                        world.LoadWorld("new" + directories.Length);

                        Main.MouseControl = false;
                        Main.DrawCursor = false;
                    }
                }
            }

            UI.EndParent();
        }

        public void Draw(SpriteBatch batch)
        {
            UI.Draw(batch, 1);
        }
    }
}
