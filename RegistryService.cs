using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Buffs;
using ViMG.Client;
using ViMG.Cubes;
using ViMG.Entities.Renderers;
using ViMG.Items;
using ViMG.Recipes;

namespace ViMG
{
	public class RegistryService
	{
		public ItemRegistry ItemRegistry;
		public CubeRegistry CubeRegistry;
		public RecipeRegistry RecipeRegistry;
		public BuffRegistry BuffRegistry;
		public RendererRegistry RendererRegistry;
		public RegistryClientEntity ClientEntityRegistry;

		public RegistryService(GraphicsDevice device)
		{
			ItemRegistry = new ItemRegistry();
			CubeRegistry = new CubeRegistry();
			RecipeRegistry = new RecipeRegistry();
			BuffRegistry = new BuffRegistry();
			RendererRegistry = new RendererRegistry(device);
			ClientEntityRegistry = new RegistryClientEntity();
		}

		public void Register()
		{
			CubeRegistry.RegisterAll();
			ItemRegistry.RegisterAll();
			RecipeRegistry.RegisterAll();
			BuffRegistry.RegisterAll();
			RendererRegistry.RegisterAll();
			ClientEntityRegistry.RegisterAll();
		}
	}
}
