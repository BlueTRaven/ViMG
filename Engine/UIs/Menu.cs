using BrUtility;
using Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ViMG.GameStates;
using ViMG.IMGUIImpl;

namespace ViMG.UIs
{
	public class Menu
	{
		protected const int PADDING = 0 * SCALE;
		protected const int MARGIN = 8 * SCALE;
		protected const int PADDING_CRAFTING = 8 * SCALE;
		protected const int MARGIN_CRAFTING = 8 * SCALE;
		protected const int SIZE = 16 * SCALE;
		protected const int SCALE = 2;

		protected readonly GameStateManager gsManager;

		public bool RespondToInput => GlobalState.Time - timeOpened > 0.125;
		protected double timeOpened = 0;

		public Menu(GameStateManager gsManager)
        {
			this.gsManager = gsManager;
        }

		public virtual void LoadContent()
		{
            // It is invalid to call LoadContent while headless
			// ... this was before I separated out client and server stuff, and now the server shouldn't ever 
			// be able to run menus. This check is kinda unnecessary...
            IMGUIConsole.Assert(!GlobalState.IsHeadless);
		}

		public virtual void OnOpen()
        {
			timeOpened = GlobalState.Time;
        }

		public virtual void OnClose()
        {

        }

		public virtual void Update(double deltaTime)
		{

		}

		public virtual void Draw(SpriteBatch batch)
		{

		}
	}
}
