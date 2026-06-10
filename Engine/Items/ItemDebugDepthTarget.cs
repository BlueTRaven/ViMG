using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Items
{
	public class ItemDebugDepthTarget : Item
	{
		public RenderTarget2D DepthTarget;

		private static SimpleMesh<VertexPositionTexture, int> depthMesh;
		public ItemDebugDepthTarget() : base("debug_depth_target")
		{
			name = "DEBUG SHADOW DEPTH RENDERER";
			description = "Renders the depth buffer into your very hands.\n" +
				"Shadows are " + (Main.ENABLE_SHADOWS ? "enabled" : "disabled") + ".";
		}
	}
}
