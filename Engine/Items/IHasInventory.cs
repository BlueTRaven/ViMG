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
        bool InventoryAction(int activatingPlayer, int action);
    }
}
