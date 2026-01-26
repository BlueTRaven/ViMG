using BrUtility;
using Engine.Clients;
using Engine.Items;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemLantern : Item
    {
        private static Vector4 color;

        static ItemLantern()
        {
            color = Color.Orange.ToVector4();
            color.W = 1.5f;
        }

        public ItemLantern() : base("lantern")
        {
        }

        public override ClientItem ClientInit()
        {
            return new ClientItemLantern(this);
        }

        public override void Hold(Player player, Inventory inventory, int index)
        {
            base.Hold(player, inventory, index);

            player.world.LightManager2.AddShadowmapped(new Engine.Common.LightManager2.LightConfig
            {
                position = player.Position,
                min = Cube.CUBE_SCALE * 4,
                max = Cube.CUBE_SCALE * 16,
                color = color,
            });
        }

        public override void EndHold(Player player, Inventory inventory, int newIndex)
        {
            base.EndHold(player, inventory, newIndex);
        }
    }

    public class ClientItemLantern : ClientItem
    {
        private Vector4 color;
        public ClientItemLantern(Item item) : base(item, new RectangleF(32, 64, 16, 16))
        {
            color = Color.Orange.ToVector4();
            color.W = 1.5f;
        }

        public override void Hold(ClientStates client, BasicState player, Inventory inventory, int index)
        {
            base.Hold(client, player, inventory, index);

            client.LightManager.AddShadowmapped(new Engine.Common.LightManager2.LightConfig
            {
                position = player.position,
                min = Cube.CUBE_SCALE * 4,
                max = Cube.CUBE_SCALE * 16,
                color = color,
            });
        }
    }
}
