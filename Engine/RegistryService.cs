using Engine.Mods;
using Engine.Networking.Messages;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Entities.Renderers;
using ViMG.Items;
using ViMG.Recipes;
using ViMG.WorldLogics;

namespace ViMG
{
	public class RegistryService
	{
        private readonly GraphicsDevice? device;

        public ModRegistry ModRegistry;
		public ItemRegistry ItemRegistry;
		public CubeRegistry CubeRegistry;
		public RecipeRegistry RecipeRegistry;
		public BuffRegistry BuffRegistry;
		public RendererRegistry RendererRegistry;
		public WorldLogicRegistry WorldLogicRegistry;
        public MessageRegistry MessageRegistry;

		public RegistryService(GraphicsDevice? device)
		{
			ModRegistry = new ModRegistry(device);
			ItemRegistry = new ItemRegistry();
			CubeRegistry = new CubeRegistry();
			RecipeRegistry = new RecipeRegistry();
			BuffRegistry = new BuffRegistry();
            if (device != null)
			    RendererRegistry = new RendererRegistry(device);
			WorldLogicRegistry = new WorldLogicRegistry();
            MessageRegistry = new MessageRegistry();
            this.device = device;
        }

		public void Register()
		{
			ModRegistry.RegisterAll();
			CubeRegistry.RegisterAll();
			ItemRegistry.RegisterAll();
			RecipeRegistry.RegisterAll();
			BuffRegistry.RegisterAll();
			RendererRegistry?.RegisterAll();
			WorldLogicRegistry.RegisterAll();
            MessageRegistry.RegisterAll();

			RecipeRegistry.PostRegistration();

			foreach (Mod mod in ModRegistry.GetIterable())
			{
				var service = mod.CreateModRegistryService(device);
                if (service != null)
                {
                    service.CubeRegistry?.RegisterAll();
                    service.CubeRegistry?.PostRegistration();
                    CubeRegistry.AddFromOther(service.CubeRegistry);
                }
            }

            CubeRegistry.PostRegistration();

            if (device != null)
            {
                foreach (Cube cube in CubeRegistry.GetIterable())
                {
                    (cube as IRegisterable).LoadContent(device);
                }
            }

            foreach (Mod mod in ModRegistry.GetIterable())
			{
                var service = mod.Registry;
                if (service != null)
                {
                    service.ItemRegistry?.RegisterAll();
                    // Don't perform post-registration as for item registries this is responsible for creating cube items.
                    // FIXME
                    // This should probably be done in a different order so this isn't a problem. What if a mod wants to override PostRegistration? They'd override it and
                    // wonder why it's not getting called only for ItemRegistry...
                    ItemRegistry.AddFromOther(service.ItemRegistry);
                }
            }

            ItemRegistry.PostRegistration();

            if (device != null)
            {
                foreach (Item item in ItemRegistry.GetIterable())
                {
                    (item as IRegisterable).LoadContent(device);
                }
            }

            foreach (Mod mod in ModRegistry.GetIterable())
            {
                var service = mod.Registry;
                if (service != null)
                {
                    service.RecipeRegistry?.RegisterAll();
                    service.RecipeRegistry?.PostRegistration();
                    RecipeRegistry.AddFromOther(service.RecipeRegistry);
                }
            }

            if (device != null)
            {
                foreach (Recipe recipe in RecipeRegistry.GetIterable())
                {
                    (recipe as IRegisterable).LoadContent(device);
                }
            }

            foreach (Mod mod in ModRegistry.GetIterable())
            {
                var service = mod.Registry;
                if (service != null)
                {
                    service.BuffRegistry?.RegisterAll();
                    service.BuffRegistry?.PostRegistration();
                    BuffRegistry.AddFromOther(service.BuffRegistry);
                }
            }

            if (device != null)
            {
                foreach (Buff buff in BuffRegistry.GetIterable())
                {
                    (buff as IRegisterable).LoadContent(device);
                }
            }

            if (RendererRegistry != null)
            {
                foreach (Mod mod in ModRegistry.GetIterable())
                {
                    var service = mod.Registry;
                    if (service != null)
                    {
                        service.RendererRegistry?.RegisterAll();
                        service.RendererRegistry?.PostRegistration();
                        RendererRegistry.AddFromOther(service.RendererRegistry);
                    }
                }
            }

            foreach (Mod mod in ModRegistry.GetIterable())
            {
                var service = mod.Registry;
                if (service != null)
                {
                    service.WorldLogicRegistry?.RegisterAll();
                    WorldLogicRegistry.AddFromOther(service.WorldLogicRegistry);
                }
            }

            foreach (Mod mod in ModRegistry.GetIterable())
			{ 
				mod.OnRegister();
			}

			ModRegistry.PostRegistration();
            BuffRegistry.PostRegistration();
            RendererRegistry?.PostRegistration();
        }

		public Mod GetCurrentMod()
		{
			return ModRegistry.Get(Assembly.GetCallingAssembly());
		}
	}
}
