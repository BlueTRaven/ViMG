using Engine;
using Engine.Items;
using Engine.Mods;
using Microsoft.Xna.Framework.Graphics;
using ModGameBase.Entities;
using ModGameBase.Networking;
using ModGameBase.Projectiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Entities.Renderers;
using ViMG.Items;
using ViMG.Recipes;
using ViMG.WorldLogics;

namespace ViMG
{
    public class ModViMG : Mod
    {
        public class ModRegistryServiceViMG : ModRegistryService
        {
            public ModRegistryServiceViMG(GraphicsDevice? device) : base(device)
            {
                this.CubeRegistry = new CubeRegistryViMG();
                this.ItemRegistry = new ItemRegistryViMG();
                this.BuffRegistry = new BuffRegistryViMG();
                this.RecipeRegistry = new RecipeRegistryViMG();
                if (device != null)
                    this.RendererRegistry = new RendererRegistryViMG(device);
                this.WorldLogicRegistry = new WorldLogicRegistryViMG();
                EntityRegistry = new EntityRegistryViMG();
                ProjectileRegistry = new ProjectileRegistryViMG();
                MessageRegistry = new MessageRegistryViMG();
            }
        }

        public override string Identifier => "ViMG";
        private ModRegistryServiceViMG registry;
        public override ModRegistryService Registry => registry;

        public override void OnRegister()
        {
            base.OnRegister();

            if (GlobalState.Registry.RendererRegistry != null)
            {
                RendererOpaqueBillboardedEntityViMG.DoRegistration(GlobalState.Registry.RendererRegistry.Get("generic_billboard") as RendererOpaqueBillboardedEntity);
                RendererOpaqueXMeshEntityViMG.DoRegistration(GlobalState.Registry.RendererRegistry.Get("xmesh") as RendererOpaqueXMeshEntity);
            }
        }

        public override ModRegistryService CreateModRegistryService(GraphicsDevice? device)
        {
            registry ??= new ModRegistryServiceViMG(device);
            return registry;
        }

        public override void AddSpawnInventoryItems(Inventory inventory)
        {
            base.AddSpawnInventoryItems(inventory);

            inventory.Add(ItemPickaxe.CreatePickaxe(new ItemInstance(GlobalState.Registry.ItemRegistry.Get("pickaxe_head_tin"), 1, 1)));
            inventory.Add(ItemSword.CreateSword(new ItemInstance(GlobalState.Registry.ItemRegistry.Get("sword_blade_tin"), 1, 1)));
        }

        public static ModRegistryServiceViMG GetRegistry()
        {
            return GlobalState.Registry.ModRegistry.Get("ViMG").Registry as ModRegistryServiceViMG;
        }
    }
}
