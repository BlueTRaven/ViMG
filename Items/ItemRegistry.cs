using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG.Items
{
	public class ItemRegistry : ObjRegistry<Item>
	{
		protected override void DoRegistration()
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
			Register(new ItemTinChunk());
			Register(new ItemCopperChunk());
			Register(new ItemIronIngot());
			Register(new ItemTinIngot());
			Register(new ItemCopperIngot());
			Register(new ItemBronzeIngot());
			Register(new ItemDebugDepthTarget());
			Register(new ItemArrow());
			Register(new ItemPickaxeHead("iron", new Color(233, 196, 196), new ItemPickaxeHead.PickaxeStats(1, 1, 1, 0)));
			Register(new ItemSwordBlade("iron", new Color(233, 196, 196), new Item.AttackStats(1, 1)));
			Register(new ItemBow("iron", new Color(233, 196, 196), new Item.AttackStats(1, 1)));
			Register(new ItemPickaxeHead("tin", Color.White, new ItemPickaxeHead.PickaxeStats(1, 0, 0, 0)));
			Register(new ItemSwordBlade("tin", Color.White, new Item.AttackStats(1, 1)));
			Register(new ItemBow("tin", Color.White, new Item.AttackStats(1, 1)));
			Register(new ItemPickaxeHead("copper", new Color(220, 187, 146), new ItemPickaxeHead.PickaxeStats(1, 0, 0, 0)));
			Register(new ItemSwordBlade("copper", new Color(220, 187, 146), new Item.AttackStats(1, 1)));
			Register(new ItemBow("copper", new Color(220, 187, 146), new Item.AttackStats(1, 1)));
			Register(new ItemPickaxeHead("bronze", new Color(233, 81, 23), new ItemPickaxeHead.PickaxeStats(1, 1, 1, 0)));
			Register(new ItemSwordBlade("bronze", new Color(233, 81, 23), new Item.AttackStats(1, 1)));
			Register(new ItemBow("bronze", new Color(233, 81, 23), new Item.AttackStats(1, 1)));
			Register(new ItemPickaxe());
			Register(new ItemSword());
			Register(new ItemAltarDust());
			Register(new ItemBrittleBone());
			Register(new ItemEnchantedBone());
			Register(new ItemInfusedBone());
			Register(new ItemDebugStructureCopier());
			Register(new ItemString());
			Register(new ItemLantern());
			RegisterItemCubes();
		}

		private void RegisterItemCubes()
		{
			IReadOnlyList<Cube> cubes = Main.Registry.CubeRegistry.GetIterable();
			for (int i = 0; i < cubes.Count; i++)
			{
				ItemCube itemCube = new ItemCube(cubes[i], (ushort)(i + 1));
				Register(itemCube);
			}
		}

		protected override void Register(Item obj)
		{
			obj.SetId(Count + 1);
			base.Register(obj);
		}
	}
}
