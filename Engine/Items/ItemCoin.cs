using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemCoin : Item
    {
        private readonly string resource;
        public readonly int Value;

        private readonly string realResourceName;
        private readonly RectangleF sourceRect;

        public ItemCoin(string resource, int value, RectangleF sourceRect) : 
            base("coin_" + resource)
        {
            this.resource = resource;
            this.Value = value;
            this.sourceRect = sourceRect;
            this.realResourceName = resource.Substring(0, 1).ToUpper() + resource.Substring(1, resource.Length - 1);
        }

        public override ClientItem ClientInit()
        {
            return new ClientItemCoin(this, sourceRect);
        }

        public override string GetName(ItemInstance item)
        {
            if (item.num > 1)
                return realResourceName + " Assarii";
            else return realResourceName + " Assarius";
        }

        public override string GetDescription(ItemInstance item)
        {
            return "A " + realResourceName + " Assarius coin.\n" +
                "It is worth " + Value + " copper Assarii.";
        }
    }

    public class ClientItemCoin : ClientItem
    {
        public ClientItemCoin(Item item, RectangleF sourceRect) : base(item, sourceRect)
        {
        }
    }
}
