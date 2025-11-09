using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ViMG.Cubes;

namespace ViMG.Items
{
	public class ItemRegistry : ObjRegistry<Item>
	{
        protected override void DoRegistration()
        {
            Register(new ItemDebugPlaceBlockWand());
            Register(new ItemDebugDepthTarget());
            Register(new ItemDebugStructureCopier());
            Register(new ItemDebugStructurePaster());
        }

        public override void PostRegistration()
        {
            base.PostRegistration();

            RegisterItemCubes();
        }

        private void RegisterItemCubes()
		{
			Span<Cube> cubes = Main.Registry.CubeRegistry.GetIterable();
			for (int i = 0; i < Main.Registry.CubeRegistry.Count; i++)
			{
				ItemCube itemCube = new ItemCube(cubes[i], (ushort)(i + 1));
				Register(itemCube);
			}
		}

		public override void Register(Item obj)
		{
			obj.SetId(Count + 1);
			base.Register(obj);
		}
	}
}
