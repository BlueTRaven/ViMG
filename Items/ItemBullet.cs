using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities.Renderers;

namespace ViMG.Items
{
	public class ItemBullet : Item
	{
		public ItemBullet() : base("bullet_base", StaticMaterials.Items, new RectangleF(0, 48, 16, 16))
		{
		}
	}
}
