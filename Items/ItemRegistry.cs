using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG.Items
{
	public class ItemRegistry : ObjRegistry<Item>
	{
		public override void RegisterAll()
		{
			Register(new ItemSwordBase());
			Register(new ItemPickaxeBase());
			Register(new ItemSlimeChunk());
			Register(new ItemGun());
			Register(new ItemBullet());
			Register(new ItemIronChunk());
			Register(new ItemWood());
			Register(new ItemFlask());
			Register(new ItemFlaskHealthPotion1());
			Register(new ItemGlowdust());
			Register(new ItemGlowNode());
			RegisterItemCubes();
		}

		private void RegisterItemCubes()
		{
			IReadOnlyList<Cube> cubes = Main.Registry.CubeRegistry.GetIterable();
			for (int i = 0; i < cubes.Count; i++)
			{
				ItemCube itemCube = new ItemCube(cubes[i], i + 1);
				Register(itemCube);
			}
		}
	}
}
