using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Items
{
    public class ItemScrollFind : Item
    {
        public ItemScrollFind() : base("scroll_find", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(112, 32, 16, 16))
        {
        }

        public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			base.RightClick(player, inventory, index, facing, out itemCooldownTime);

			if (player.Magic < 5)
				return false;

			const float MIN_DISTANCE = Cube.CUBE_SCALE * 32;
			float min = float.MaxValue;
			PointOfInterest point = new PointOfInterest();

			foreach (var poi in player.world.PointsOfInterest)
            {
				Vector3 distance = player.Position - poi.position.InWorldSpace(null);

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
				player.world.EntityManager.Add(new Entities.Line(player.Position, point.position.InWorldSpace(null), 
					DrawHelper.WhitePixel, RectangleF.Empty, Color.Red, 2f * 60f));

				player.Magic -= 5;

				itemCooldownTime = 0.25f;

				return true;
			}

			return false;
		}
    }
}
