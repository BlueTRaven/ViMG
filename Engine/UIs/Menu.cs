using BrUtility;
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

		public Menu(GameStateManager gsManager)
        {
			this.gsManager = gsManager;
        }

		public virtual void LoadContent()
		{
            // It is invalid to call LoadContent while headless
            IMGUIConsole.Assert(!Main.IsHeadless);
		}

		public virtual void OnOpen()
        {

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
