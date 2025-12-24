using Engine.Items;
using Engine.Networking.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Items;

namespace Engine.Clients.Entities
{
    public class ClientInventoryDummy : Inventory
    {
        private ItemInstance item = new();

        public ClientInventoryDummy() : base(new())
        {
        }

        public override ref readonly ItemInstance Get(int index)
        {
            return ref item;
        }

        public override void DoUpdateAction(SyncInventoryUpdate.QueuedInventoryUpdate action)
        {
            
        }
    }
}
