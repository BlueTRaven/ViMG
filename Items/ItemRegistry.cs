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
			Color colorIron = new Color(233, 196, 196);
			Color colorTin = Color.White;
			Color colorCopper = new Color(220, 187, 146);
			Color colorBronze = new Color(233, 81, 23);

			SetBonusMaterial setBonusIron = new SetBonusMaterial("iron", new Player.AccumulatedStats() { Defense = 3 });
			SetBonusMaterial setBonusTin = new SetBonusMaterial("tin", new Player.AccumulatedStats() { Defense = 1 });
			SetBonusMaterial setBonusCopper = new SetBonusMaterial("copper", new Player.AccumulatedStats() { Defense = 1 });
			SetBonusMaterial setBonusBronze = new SetBonusMaterial("bronze", new Player.AccumulatedStats() { Defense = 2 });

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
			Register(new ItemPickaxeHead("iron", colorIron, new ItemPickaxeHead.PickaxeStats(1, 1, 1, 0)));
			Register(new ItemSwordBlade("iron", colorIron, new Item.AttackStats(0.90f, 5)));
			Register(new ItemBow("iron", colorIron, new Item.AttackStats(0.95f, 4)));
			Register(new ItemPickaxeHead("tin", colorTin, new ItemPickaxeHead.PickaxeStats(1, 0, 0, 0)));
			Register(new ItemSwordBlade("tin", colorTin, new Item.AttackStats(1, 3)));
			Register(new ItemBow("tin", colorTin, new Item.AttackStats(1, 2)));
			Register(new ItemPickaxeHead("copper", colorCopper, new ItemPickaxeHead.PickaxeStats(0.95f, 0, 0, 0)));
			Register(new ItemSwordBlade("copper", colorCopper, new Item.AttackStats(0.95f, 3)));
			Register(new ItemBow("copper", colorCopper, new Item.AttackStats(1, 2)));
			Register(new ItemPickaxeHead("bronze", colorBronze, new ItemPickaxeHead.PickaxeStats(1.15f, 1, 1, 0)));
			Register(new ItemSwordBlade("bronze", colorBronze, new Item.AttackStats(0.9f, 4)));
			Register(new ItemBow("bronze", colorBronze, new Item.AttackStats(0.95f, 3)));
			Register(new ItemPickaxe());
			Register(new ItemSword());
			Register(new ItemAltarDust());
			Register(new ItemBrittleBone());
			Register(new ItemEnchantedBone());
			Register(new ItemInfusedBone());
			Register(new ItemDebugStructureCopier());
			Register(new ItemString());
			Register(new ItemLantern());
			Register(new ItemMetalHelmet("iron", colorIron, new Player.AccumulatedStats() { Defense = 2 }, setBonusIron));
			Register(new ItemMetalChestplate("iron", colorIron, new Player.AccumulatedStats() { Defense = 3 }, setBonusIron));
			Register(new ItemMetalLegs("iron", colorIron, new Player.AccumulatedStats() { Defense = 2 }, setBonusIron));
			Register(new ItemMetalHelmet("tin", colorTin, new Player.AccumulatedStats() { Defense = 1 }, setBonusTin));
			Register(new ItemMetalChestplate("tin", colorTin, new Player.AccumulatedStats() { Defense = 1 }, setBonusTin));
			Register(new ItemMetalLegs("tin", colorTin, new Player.AccumulatedStats() { Defense = 0 }, setBonusTin));
			Register(new ItemMetalHelmet("copper", colorCopper, new Player.AccumulatedStats() { Defense = 1 }, setBonusCopper));
			Register(new ItemMetalChestplate("copper", colorCopper, new Player.AccumulatedStats() { Defense = 1 }, setBonusCopper));
			Register(new ItemMetalLegs("copper", colorCopper, new Player.AccumulatedStats() { Defense = 0 }, setBonusCopper));
			Register(new ItemMetalHelmet("bronze", colorBronze, new Player.AccumulatedStats() { Defense = 2 }, setBonusBronze));
			Register(new ItemMetalChestplate("bronze", colorBronze, new Player.AccumulatedStats() { Defense = 2 }, setBonusBronze));
			Register(new ItemMetalLegs("bronze", colorBronze, new Player.AccumulatedStats() { Defense = 2 }, setBonusBronze));
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
