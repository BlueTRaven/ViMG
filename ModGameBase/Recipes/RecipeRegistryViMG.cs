using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Recipes
{
    public class RecipeRegistryViMG : RecipeRegistry
    {
        public CatalystPlayerInventory PlayerInventoryCatalyst;
        public CatalystAnvilIronArmor AnvilIronArmorCatalyst;
        public CatalystAnvilIronTools AnvilIronToolsCatalyst;

        protected override void DoRegistration()
        {
            PlayerInventoryCatalyst = new CatalystPlayerInventory();
            AnvilIronArmorCatalyst = new CatalystAnvilIronArmor();
            AnvilIronToolsCatalyst = new CatalystAnvilIronTools();
            RegisterCatalyst(PlayerInventoryCatalyst);
            RegisterCatalyst(AnvilIronArmorCatalyst);
            RegisterCatalyst(AnvilIronToolsCatalyst);
        }
    }
}
