using Engine.Clients.Entities;
using Engine.Items;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Clients
{
    public class ClientInventoryManager
    {
        private struct InventoryHolder
        {
            public int id;
            public int generation;
            public Inventory? inventory;

            public bool active;

            public static InventoryHolder INVALID = new InventoryHolder { active = false, id = -1, generation = -1, inventory = null };
        }
        private InventoryHolder[] inventories;

        private ClientInventoryDummy defaultInventory;

        public ClientInventoryManager()
        {
            inventories = new InventoryHolder[InventoryManager.InvMax];
            Array.Fill(inventories, new());

            for (int i = 0; i < InventoryManager.InvMax; i++)
            {
                inventories[i] = InventoryHolder.INVALID;
            }

            defaultInventory = new ClientInventoryDummy();
        }

        public void Set(InventoryManager.InventoryReference reference, Inventory? inventory)
        {
            if (inventory == null)
            {
                inventories[reference.id - 1] = new InventoryHolder
                {
                    active = false,
                    generation = reference.generation,
                    id = reference.id,
                    inventory = null,
                };
            }
            else
            {
                inventories[reference.id - 1] = new InventoryHolder
                {
                    active = true,
                    generation = reference.generation,
                    id = reference.id,
                    inventory = inventory,
                };
            }
        }

        public Inventory Get(InventoryManager.InventoryReference reference)
        {
            if (inventories[reference.id - 1].generation != reference.generation) return defaultInventory;
            return inventories[reference.id - 1].inventory ?? defaultInventory;
        }
    }
}
