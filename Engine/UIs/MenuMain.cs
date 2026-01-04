using BrUtility;
using Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
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
            WorldCreate,
            Multiplayer,
            Settings
        }

        private MenuState state;

        private string[] directories;

        private bool clicked = false;

        private TextHelper.FontInfo fi;

        private string worldName;
        private bool startAsServer;
        private int serverPort = 9050;
        private string serverIp = "localhost";
        private string playerName = "";

        public MenuMain(GameStateManager gsManager) : base(gsManager)
        {
            Main.MouseControl = true;
            Main.DrawCursor = true;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
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

        public static string GetDefaultPlayerName(GameStateManager gsManager)
        {
            if (gsManager.netMode == GameStateManager.NetworkingMode.Singleplayer)
                return Main.SessionInformation.LastLoadedSave;
            else
                return Main.Args.playerName ?? gsManager.netMode.ToString();
        }

        public override void Update(double deltaTime)
        {
            UI.Start();

            UI.StartParent(new Vector2(Options.CurrentWindowResolution.X / 2 - 64, Options.CurrentWindowResolution.Y / 2 - 128));

            if (Main.inputManager.JustReleased(A1r.Input.MouseInput.LeftButton))
                clicked = false;

            if (state == MenuState.Main)
            {
                float ypos = 0;
                if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, 48 * ypos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"), 
                    new UI.LabelConstructionParameters("Single Player", fi, 128, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                {
                    state = MenuState.Worlds;
                    directories = GetWorldSaveDirectories();
                    clicked = true;
                }
                ypos++;

                if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, 48 * ypos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new UI.LabelConstructionParameters("Continue", fi, 128, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                {
                    gsManager.Continue(GameStateManager.NetworkingMode.Singleplayer);
                }
                ypos++;

                if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(128 + 16, 48 * (ypos - 1), 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new UI.LabelConstructionParameters("Continue (Server)", fi, 128, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                {
                    gsManager.Continue(GameStateManager.NetworkingMode.Server);
                }

                if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(128 + 16, 48 * ypos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new UI.LabelConstructionParameters("Continue (Client)", fi, 128, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                {
                    gsManager.Continue(GameStateManager.NetworkingMode.Client);
                }

                if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, 48 * ypos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new UI.LabelConstructionParameters("Multiplayer", fi, 128, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                {
                    state = MenuState.Multiplayer;
                    clicked = true;
                }
                ypos++;

                if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, 48 * ypos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new UI.LabelConstructionParameters("Options", fi, 128, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                {
                    gsManager.GetCurrentGameState().PushMenu(new MenuOptions(gsManager));
                }
                ypos++;

                if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, 48 * ypos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new UI.LabelConstructionParameters("Exit", fi, 128, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                {
                    Main.Exit = true;
                }
                ypos++;
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
                            gsManager.netMode = startAsServer ? GameStateManager.NetworkingMode.Server : GameStateManager.NetworkingMode.Singleplayer;
                            if (gsManager.netMode == GameStateManager.NetworkingMode.Singleplayer)
                                gsManager.TheIsland.localPlayerName = directories[i];

                            gsManager.SetGameState(gsManager.TheIsland);
                            gsManager.TheIsland.Connect(serverIp, serverPort);
                            if (startAsServer)
                                gsManager.TheIsland.StartSingleplayer(directories[i]);
                            else gsManager.TheIsland.StartSingleplayer(directories[i]);
                        }
                    }

                    int fypos = 48 * directories.Length;

                    if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, fypos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                        new UI.LabelConstructionParameters("Create New", fi, 128, Vector2.Zero),
                        new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                    {
                        state = MenuState.WorldCreate;
                        clicked = true;

                        worldName = "";
                    }
                }
            }
            else if (state == MenuState.WorldCreate)
            {
                if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(-32, 256, 32, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                       new UI.LabelConstructionParameters("<", fi, 32, Vector2.Zero),
                       new RectangleF(0, 64, 32, 32), new RectangleF(32, 64, 32, 32), new RectangleF(32, 64, 32, 32))).clickLeft)
                {
                    state = MenuState.Worlds;
                    clicked = true;
                }

                UI.MakeLabel(new UI.LabelConstructionParameters("World Name:", fi, 1000, new Vector2(-108, 48)));
                UI.MakeTextbox(new UI.ButtonConstructionParameters(new RectangleF(0, 48, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32)), ref worldName, UI.TextInputFlags.AlphaNumerical, fi);

                if (!clicked && UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, 48 + 48, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new UI.LabelConstructionParameters("Create World", fi, 196, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                {
                    gsManager.netMode = startAsServer ? GameStateManager.NetworkingMode.Server : GameStateManager.NetworkingMode.Singleplayer;
                    gsManager.TheIsland.netManagerServer.Port = serverPort;

                    if (worldName == "")
                        worldName = "new" + directories.Length;
                    if (gsManager.netMode == GameStateManager.NetworkingMode.Singleplayer)
                        gsManager.TheIsland.localPlayerName = worldName;

                    gsManager.SetGameState(gsManager.TheIsland);
                    gsManager.TheIsland.Connect(serverIp, serverPort);
                    if (startAsServer)
                        gsManager.TheIsland.StartSingleplayer(worldName);
                    else gsManager.TheIsland.StartSingleplayer(worldName);
                }
            }
            else if (state == MenuState.Multiplayer)
            {
                if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(-32, 256, 32, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                       new UI.LabelConstructionParameters("<", fi, 32, Vector2.Zero),
                       new RectangleF(0, 64, 32, 32), new RectangleF(32, 64, 32, 32), new RectangleF(32, 64, 32, 32))).clickLeft)
                {
                    state = MenuState.Main;
                    clicked = true;
                }

                float ypos = 0;

                UI.MakeLabel(new UI.LabelConstructionParameters("Player Name:", fi, 1000, new Vector2(-108, 48 * ypos)));
                UI.MakeTextbox(new UI.ButtonConstructionParameters(new RectangleF(0, 48 * ypos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32)), ref playerName, UI.TextInputFlags.AlphaNumerical, fi);
                ypos++;

                UI.MakeLabel(new UI.LabelConstructionParameters("IP:", fi, 1000, new Vector2(-108, 48 * ypos)));
                UI.MakeTextbox(new UI.ButtonConstructionParameters(new RectangleF(0, 48 * ypos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32)), ref serverIp, UI.TextInputFlags.AlphaNumericalSpecial, fi);
                ypos++;

                UI.MakeLabel(new UI.LabelConstructionParameters("Port:", fi, 1000, new Vector2(-108, 48 * ypos)));
                string portStr = serverPort.ToString();
                UI.MakeTextbox(new UI.ButtonConstructionParameters(new RectangleF(0, 48 * ypos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32)), ref portStr, UI.TextInputFlags.Numerical, fi);
                int.TryParse(portStr, out serverPort);
                ypos++;

                if (!clicked && UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, 48 * ypos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new UI.LabelConstructionParameters("Start Server", fi, 196, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                {
                    startAsServer = true;
                    gsManager.TheIsland.localPlayerName = playerName;
                    state = MenuState.Worlds;
                    directories = GetWorldSaveDirectories();
                    clicked = true;
                }
                ypos++;

                if (!clicked && UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, 48 * ypos, 128, 32), Main.assetsManager.GetAsset<Texture2D>("ui_buttons"),
                    new UI.LabelConstructionParameters("Connect", fi, 196, Vector2.Zero),
                    new RectangleF(0, 0, 128, 32), new RectangleF(0, 32, 128, 32), new RectangleF(0, 32, 128, 32))).clickLeft)
                {
                    gsManager.netMode = GameStateManager.NetworkingMode.Client;
                    gsManager.TheIsland.localPlayerName = playerName;
                    gsManager.SetGameState(gsManager.TheIsland);
                    gsManager.TheIsland.Connect(serverIp, serverPort);
                    gsManager.TheIsland.StartClient();
                }
                ypos++;
            }
            UI.EndParent();
        }

        public override void Draw(SpriteBatch batch)
        {
            UI.Draw(batch, 1);
        }

        private string[] GetWorldSaveDirectories()
        {
            string[] strings;

            if (Directory.Exists(WorldIO.SAVE_FOLDER))
                strings = Directory.GetDirectories(WorldIO.SAVE_FOLDER);
            else strings = Array.Empty<string>();

            for (int i = 0; i < strings.Length; i++)
            {
                int ind = strings[i].LastIndexOf('/');
                strings[i] = strings[i].Substring(ind + 1);
            }

            return strings;
        }
    }
}
