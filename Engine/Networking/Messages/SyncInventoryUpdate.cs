using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Items;

namespace Engine.Networking.Messages
{
    public class SyncInventoryUpdate : Message
    {
        public static SyncInventoryUpdate Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        private struct QueuedInventoryUpdate
        {
            public int player;
            public int inventoryId;
            public ItemInstance oldInstance, newInstance;
            public double time;
        }
        private List<QueuedInventoryUpdate> queued1 = new();
        private List<QueuedInventoryUpdate> queued2 = new();
        private List<QueuedInventoryUpdate> queued;

        public SyncInventoryUpdate()
        {
            Instance = this;
            queued = queued1;
        }
    }
}
