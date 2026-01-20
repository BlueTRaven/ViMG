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

        public virtual void Initialize()
        {

        }

        public virtual void LoadContent(GraphicsDevice device)
        {

        }

        public virtual void Update(double deltaTime)
        {
            bool doDisable = !currentMenu?.RespondToInput ?? false;
            if (doDisable)
                UI.BeginDisable();
            currentMenu?.Update(deltaTime);
            if (doDisable)
                UI.EndDisable();
        }

        public virtual void DrawUI(SpriteBatch batch)
        {
            currentMenu?.Draw(batch);
        }

        public virtual void Draw(GraphicsDevice device, SpriteBatch batch, double deltaTime)
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

        //Push the current menu to the stack, then set current menu to the parameter menu.
        public void PushMenu(Menu menu)
        {
            currentMenu?.OnClose();
            menuStack.Push(currentMenu);

            currentMenu = menu;

            currentMenu?.OnOpen();
        }

        public void PopMenu()
        {
            currentMenu?.OnClose();

            Menu nextMenu = menuStack.Pop();

            currentMenu = nextMenu;

            currentMenu?.OnOpen();
        }

        //Directly set a menu. Clears the stack.
        public void SetMenu(Menu menu)
        {
            currentMenu?.OnClose();

            menuStack.Clear();
            this.currentMenu = menu;
            menuStack.Push(menu);

            currentMenu?.OnOpen();
        }

        public Menu GetCurrentMenu()
        {
            return currentMenu;
        }
    }
}
