using BrUtility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;
using ViMG.IMGUIImpl;

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
        }

        private struct InventoryHolder
        {
            public int id;
            public int generation;
            public Inventory inventory;

            public bool active;
        }
        private InventoryHolder[] inventories;
        private List<int> freeList;

        public InventoryManager()
        {
            inventories = new InventoryHolder[InvMax];
            Array.Fill(inventories, new());

            freeList = new List<int>();

            for (int i = InvMax - 1; i >= 0; i--)
            {
                freeList.Add(i);
            }

            Debug.Assert(freeList.First() == InvMax - 1);
        }

        public int GetUniqueId()
        {
            if (freeList.Count == 0) return -1;
            int last = freeList.Last();
            freeList.RemoveAt(freeList.Count - 1);

            return last;
        }

        public void Add(int numSlots)
        {
            int id = GetUniqueId();

            Debug.Assert(!inventories[id].active);

            inventories[id] = new InventoryHolder
            {
                id = id,
                generation = inventories[id].generation,
                active = true,
                inventory = new Inventory(id, numSlots),
            };
        }

        public void Unload(int id)
        {
            inventories[id] = inventories[id] with
            {
                active = false,
                inventory = null,
                generation = inventories[id].generation + 1,
            };
        }

        public Inventory? Get(InventoryReference reference)
        {
            if (inventories[reference.id].generation != reference.generation) return null;
            return inventories[reference.id].inventory;
        }
    }
}
