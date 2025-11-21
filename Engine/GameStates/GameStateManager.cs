using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.UIs;

namespace ViMG.GameStates
{
    public class GameStateManager
    {
        public enum NetworkingMode
        {
            Server, // Acting as host. Can play
            Client, // Acting as client
            Singleplayer, // Singleplayer. 
        }

        private List<GameState> gameStates = new List<GameState>();

        public GameStateMainMenu MainMenu;
        public GameStateTheIsland TheIsland;

        public bool Paused;

        private GameState currentGameState = null;

        public NetworkingMode netMode = NetworkingMode.Singleplayer;

        public virtual void Initialize()
        {
            MainMenu = new GameStateMainMenu(this);
            TheIsland = new GameStateTheIsland(this);

            gameStates.Add(MainMenu);
            gameStates.Add(TheIsland);

            gameStates.ForEach(x => x.Initialize());

            SetGameState(MainMenu);
        }

        public virtual void LoadContent(GraphicsDevice device)
        {
            gameStates.ForEach(x => x.LoadContent(device));
            // MenuMain.LoadContent is called by SetGameState->OnOpen, but occurs before we actually load our assets
            MainMenu.LoadContent(device);
        }

        private static bool parsedArgs = false;
        public void Update(double deltaTime)
        {
            if (!parsedArgs && Main.Args.startMode == "TheIsland")
            {
                var netMode = Enum.Parse<NetworkingMode>(Main.Args.networkingMode);
                Continue(netMode);
                parsedArgs = true;
            }

            currentGameState?.Update(deltaTime);
        }

        public void DrawUI(SpriteBatch batch)
        {
            currentGameState?.DrawUI(batch);
        }

        public void Draw(GraphicsDevice device)
        {
            currentGameState?.Draw(device);
        }

        public GameState GetCurrentGameState()
        {
            return currentGameState;
        }

        public void SetGameState(GameState state)
        {
            GameState oldState = currentGameState;
            currentGameState?.OnClose(state);
            currentGameState = state;
            currentGameState?.OnOpen(oldState);
        }

        public void Continue(NetworkingMode netMode)
        {
            if (Main.SessionInformation.LastLoadedSave != null)
            {
                this.netMode = netMode;
                SetGameState(TheIsland);
                if (netMode == NetworkingMode.Singleplayer || netMode == NetworkingMode.Server)
                {
                    TheIsland.BeginLoadWorld(Main.SessionInformation.LastLoadedSave);
                }
                else
                {
                    TheIsland.LoadNone();
                }
            }
        }
    }
}
