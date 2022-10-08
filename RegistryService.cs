using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Buffs;
using ViMG.Cubes;
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

		public RegistryService()
		{
			ItemRegistry = new ItemRegistry();
			CubeRegistry = new CubeRegistry();
			RecipeRegistry = new RecipeRegistry();
			BuffRegistry = new BuffRegistry();
		}

		public void Register()
		{
			CubeRegistry.RegisterAll();
			ItemRegistry.RegisterAll();
			RecipeRegistry.RegisterAll();
			BuffRegistry.RegisterAll();
		}
	}
}
