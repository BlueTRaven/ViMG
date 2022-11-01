using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.GameStates
{
    public class GameStateManager
    {
        private List<GameState> gameStates = new List<GameState>();

        public GameStateMainMenu MainMenu;
        public GameStateTheIsland TheIsland;

        public bool Paused;

        private GameState currentGameState;

        public virtual void Initialize(GraphicsDevice device)
        {
            MainMenu = new GameStateMainMenu(this, device);
            TheIsland = new GameStateTheIsland(this);

            gameStates.Add(MainMenu);
            gameStates.Add(TheIsland);

            gameStates.ForEach(x => x.Initialize(device));

            SetGameState(MainMenu);
        }

        public void Update(GraphicsDevice device, double deltaTime)
        {
            currentGameState?.Update(device, deltaTime);
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
    }
}
