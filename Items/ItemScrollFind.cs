using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities.Renderers;

namespace ViMG.Items
{
    public class ItemScrollFind : Item
    {
        public ItemScrollFind() : base("scroll_find", StaticMaterials.Items, new RectangleF(112, 32, 16, 16))
        {
        }

        public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
		{
			base.RightClick(player, inventory, index, facing, out actionStats);

			if (player.Magic < 5)
				return false;

			const float MIN_DISTANCE = Cube.CUBE_SCALE * 32;
			float min = float.MaxValue;
			PointOfInterest point = new PointOfInterest();

			foreach (var poi in player.world.WorldInfo.pointsOfInterest)
            {
				Vector3 distance = player.Position - poi.position.InWorldSpace();

				if (distance.Length() <= MIN_DISTANCE)
                {
					if (distance.Length() < min)
                    {
						min = distance.Length();
						point = poi;
                    }
                }
            }

			if (point.valid) 
			{
				player.world.EntityManager.Add(new Entities.Line(player.Position, point.position.InWorldSpace(), Cube.CUBE_SCALE / 16f, -1,
					new Rendering.RendererDeferred.DrawMaterial(DrawHelper.WhitePixel), RectangleF.Empty, Color.Red, 2f * 60f));

				player.Magic -= 5;

				actionStats.useTime = 0.25f;
				actionStats.useAnimTime = 0.25f;

				return true;
			}

			return false;
		}
    }
}
