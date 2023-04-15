using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities.Renderers;

namespace ViMG.Items
{
	public class ItemFlask : Item
	{
		public ItemFlask() : base("flask_empty", StaticMaterials.Items, new RectangleF(0, 96, 16, 16))
		{
			name = "Empty Flask";
			description = "An empty flask without substance to fill its void.";
		}
	}
}
