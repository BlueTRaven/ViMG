using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities.Renderers;

namespace ViMG.Items
{
	public class ItemAltarDust : Item
	{
		public ItemAltarDust() : base("altar_dust", StaticMaterials.Items, new RectangleF(0, 16, 16, 16))
		{
			name = "Ancient Altar Dust";
			description = "Dust from an altar so ancient that merely touching it causes it to disintigrate. It is infused with a strange, otherworldly energy.";
		}
	}
}
