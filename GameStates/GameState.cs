using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.UIs;

namespace ViMG.GameStates
{
    public abstract class GameState
    {
        private Stack<Menu> menuStack = new Stack<Menu>();
        private Menu currentMenu;
        protected readonly GameStateManager manager;

        public GameState(GameStateManager manager)
        {
            this.manager = manager;
        }

        public virtual void Initialize(GraphicsDevice device)
        {

        }

        public virtual void Update(GraphicsDevice device, double deltaTime)
        {
            currentMenu?.Update(device, deltaTime);
        }

        public virtual void DrawUI(SpriteBatch batch)
        {
            currentMenu?.Draw(batch);
        }

        public virtual void Draw(GraphicsDevice device)
        {

        }

        public virtual void OnClose(GameState changingTo)
        {
            currentMenu?.OnClose();
        }

        public virtual void OnOpen(GameState changingFrom)
        {
            currentMenu?.OnOpen();
        }

        public void PushMenu(Menu menu)
        {
            menuStack.Push(menu);

            currentMenu = menu;
        }

        public void PopMenu()
        {
            Menu nextMenu = menuStack.Pop();

            currentMenu = nextMenu;
        }

        //Directly set a menu. Clears the stack.
        public void SetMenu(Menu menu)
        {
            menuStack.Clear();
            this.currentMenu = menu;
            menuStack.Push(menu);
        }

        public Menu GetCurrentMenu()
        {
            return currentMenu;
        }
    }
}
