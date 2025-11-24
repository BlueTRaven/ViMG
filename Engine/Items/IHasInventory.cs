using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Items
{
    public interface IHasInventory
    {
        Inventory GetInventory(int id);

        bool InventoryAction(Player? activatingPlayer, int action);
    }
}
