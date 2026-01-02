using BrUtility;
using Engine.Networking.Messages;
using LiteNetLib.Utils;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;
using ViMG.IMGUIImpl;
using static ViMG.UIs.UI;

namespace Engine.Items
{
    public class InventoryManager
    {
        [ConsoleCommandVar("inv_max", "Maximum number of inventories")]
        public static int InvMax = 4096;

        public readonly struct InventoryReference
        {
            public readonly int id;
            public readonly int generation;

            public InventoryReference(int id, int generation)
            {
                this.id = id;
                this.generation = generation;
            }
            public static InventoryReference INVALID = new InventoryReference(0, -1);

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(id);
                writer.Put(generation);
            }

            public static InventoryReference Deserialize(NetDataReader reader)
            {
                int id = reader.GetInt();
                int generation = reader.GetInt();

                return new InventoryReference(id, generation);
            }
        }

        private struct InventoryHolder
        {
            public int id;
            public int generation;
            public Inventory inventory;

            public bool active;

            public static InventoryHolder INVALID = new InventoryHolder { active = false, id = -1, generation = -1, inventory = null };
        }
        private InventoryHolder[] inventories;
        private List<int> freeList;

        private FastList<InventoryReference> newInventories = new();
        private FastList<InventoryReference> remInventories = new();

        public InventoryManager()
        {
            inventories = new InventoryHolder[InvMax];
            Array.Fill(inventories, new());

            freeList = new List<int>();

            for (int i = InvMax - 1; i >= 0; i--)
            {
                freeList.Add(i);
                inventories[i] = InventoryHolder.INVALID;
            }

            Debug.Assert(freeList.First() == InvMax - 1);
        }

        public int GetUniqueId()
        {
            if (freeList.Count == 0) return 0;
            int last = freeList.Last();
            freeList.RemoveAt(freeList.Count - 1);

            return last + 1;
        }

        public InventoryReference Add(Inventory.InventoryConfig config)
        {
            int id = GetUniqueId();

            Debug.Assert(!inventories[id - 1].active);

            inventories[id - 1] = new InventoryHolder
            {
                id = id,
                generation = inventories[id - 1].generation + 1,
                active = true,
                inventory = new Inventory(config with { id = id }),
            };

            var reference = GetReference(id - 1);
            newInventories.Add(reference);

            return reference;
        }

        public void Unload(InventoryReference reference)
        {
            if (reference.generation != inventories[reference.id - 1].generation)
            {
                Console.WriteLine("Tried to unload inventory but generation was wrong");
                return;
            }

            inventories[reference.id - 1] = inventories[reference.id - 1] with
            {
                active = false,
                inventory = null,
                generation = inventories[reference.id - 1].generation + 1,
            };

            remInventories.Add(reference);
        }

        public InventoryReference GetReference(int id)
        {
            return new InventoryReference(id + 1, inventories[id].generation);
        }

        public Inventory? Get(InventoryReference reference)
        {
            if (inventories[reference.id - 1].generation != reference.generation) return null;
            return inventories[reference.id - 1].inventory;
        }

        public Inventory GetOrAdd(ref InventoryReference reference, Inventory.InventoryConfig config)
        {
            if (reference.id == 0)
            {
                var id = GetUniqueId();
                reference = new InventoryReference(id, inventories[id - 1].generation);
                inventories[reference.id - 1] = new InventoryHolder
                {
                    id = reference.id,
                    generation = inventories[reference.id - 1].generation,
                    active = true,
                    inventory = new Inventory(config with { id = reference.id }),
                };
            }
        
            return inventories[reference.id - 1].inventory;
        }

        private FastList<Inventory> cachedNewInv = new();
        private FastList<InventoryReference> cachedNewInvRef = new();
        public void UpdateNetwork(Player[] player)
        {
            //foreach (InventoryReference reference in newInventories.Slice())
            //{
            //    Inventory? inv = Get(reference);
            //    if (inv != null)
            //    {
            //        cachedNewInv.Add(inv);
            //        cachedNewInvRef.Add(reference);
            //    }
            //}

            SyncInventory.Instance.DoSync(this, player);
            //SyncInventoryAdd.Instance.DoSync(cachedNewInv, cachedNewInvRef);

            //SyncInventoryRemove.Instance.DoSync(remInventories);
            newInventories.Clear();
            remInventories.Clear();

            cachedNewInv.Clear();
            cachedNewInvRef.Clear();
        }
    }
}
