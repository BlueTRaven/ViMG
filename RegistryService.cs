using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Items;

namespace ViMG
{
	public class RegistryService
	{
		public ItemRegistry ItemRegistry;
		public CubeRegistry CubeRegistry;

		public RegistryService()
		{
			ItemRegistry = new ItemRegistry();
			CubeRegistry = new CubeRegistry();
		}

		public void Register()
		{
			CubeRegistry.RegisterAll();
			ItemRegistry.RegisterAll();
		}
	}
}
