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

			Color colorBone = new Color(191, 191, 139);
			Color colorOldIron = new Color(104, 76, 84);

			SetBonusMaterial setBonusIron = new SetBonusMaterial("iron", new Player.AccumulatedStats() { DefenseFlat = 3 });
			SetBonusMaterial setBonusTin = new SetBonusMaterial("tin", new Player.AccumulatedStats() { DefenseFlat = 1 });
			SetBonusMaterial setBonusCopper = new SetBonusMaterial("copper", new Player.AccumulatedStats() { DefenseFlat = 1 });
			SetBonusMaterial setBonusBronze = new SetBonusMaterial("bronze", new Player.AccumulatedStats() { DefenseFlat = 2 });

			SetBonusMaterial setBonusBone = new SetBonusMaterial("bone", new Player.AccumulatedStats() { MeleeAtkScale = 0.05f });
			SetBonusMaterial setBonusOldIron = new SetBonusMaterial("oldiron", new Player.AccumulatedStats() { RangeAtkScale = 0.15f });

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
			Register(new ItemArrowStone());
			Register(new ItemPickaxeHead("iron", colorIron, new ItemPickaxeHead.PickaxeStats(0.40f, 0, 1, 1, 1, 0)));
			Register(new ItemSwordBlade("iron", colorIron, new Item.MeleeAttackStats(new Item.AttackStats(Player.DamageType.Melee, 
				new Player.ActionStats() 
				{ 
					useTime = 0.85f,
                    useAnimTime = 8f / 60f,
                    preUseTime = 8f / 60f,
                }, 7, Cube.CUBE_SCALE), Cube.CUBE_SCALE * 2f)));
			Register(new ItemBow("iron", colorIron, new Item.RangedAttackStats(new Item.AttackStats(Player.DamageType.Ranged, 
				0.85f, 6, Cube.CUBE_SCALE), Cube.CUBE_SCALE * 23f, 0.85f)));
			Register(new ItemPickaxeHead("tin", colorTin, new ItemPickaxeHead.PickaxeStats(0.5f, 0, 1, 0, 0, 0)));
			Register(new ItemSwordBlade("tin", colorTin, new Item.MeleeAttackStats(new Item.AttackStats(Player.DamageType.Melee,
				new Player.ActionStats()
				{
					useTime = 1f,
					useAnimTime = 8f / 60f,
                    preUseTime = 8f / 60f,
                }, 3, Cube.CUBE_SCALE), Cube.CUBE_SCALE * 1.45f)));
			Register(new ItemBow("tin", colorTin, new Item.RangedAttackStats(new Item.AttackStats(Player.DamageType.Ranged, 1, 2, Cube.CUBE_SCALE), Cube.CUBE_SCALE * 15f, 1f)));
			Register(new ItemPickaxeHead("copper", colorCopper, new ItemPickaxeHead.PickaxeStats(0.475f, 0, 1, 0, 0, 0)));
			Register(new ItemSwordBlade("copper", colorCopper, new Item.MeleeAttackStats(new Item.AttackStats(Player.DamageType.Melee,
				new Player.ActionStats()
				{
					useTime = 0.95f,
					useAnimTime = 8f / 60f,
                    preUseTime = 8f / 60f,
                }, 3, Cube.CUBE_SCALE), Cube.CUBE_SCALE * 1.55f)));
			Register(new ItemBow("copper", colorCopper, new Item.RangedAttackStats(new Item.AttackStats(Player.DamageType.Ranged, 1, 2, Cube.CUBE_SCALE), Cube.CUBE_SCALE * 15f, 1f)));
			Register(new ItemPickaxeHead("bronze", colorBronze, new ItemPickaxeHead.PickaxeStats(0.575f, 0, 1, 1, 1, 0)));
			Register(new ItemSwordBlade("bronze", colorBronze, new Item.MeleeAttackStats(new Item.AttackStats(Player.DamageType.Melee, 
				new Player.ActionStats()
				{
					useTime = 0.9f,
					useAnimTime = 8f / 60f,
                    preUseTime = 8f / 60f,
                }, 4, Cube.CUBE_SCALE), Cube.CUBE_SCALE * 1.85f)));
			Register(new ItemBow("bronze", colorBronze, new Item.RangedAttackStats(new Item.AttackStats(Player.DamageType.Ranged, 0.95f, 3, Cube.CUBE_SCALE), Cube.CUBE_SCALE * 18f, 0.85f)));
			Register(new ItemPickaxe());
			Register(new ItemSword());
			Register(new ItemAltarDust());
			Register(new ItemBrittleBone());
			Register(new ItemEnchantedBone());
			Register(new ItemInfusedBone());
			Register(new ItemDebugStructureCopier());
			Register(new ItemString());
			Register(new ItemLantern());
			Register(new ItemMetalHelmet("iron", colorIron, new Player.AccumulatedStats() { DefenseFlat = 2 }, setBonusIron));
			Register(new ItemMetalChestplate("iron", colorIron, new Player.AccumulatedStats() { DefenseFlat = 3 }, setBonusIron));
			Register(new ItemMetalLegs("iron", colorIron, new Player.AccumulatedStats() { DefenseFlat = 2 }, setBonusIron));
			Register(new ItemMetalHelmet("tin", colorTin, new Player.AccumulatedStats() { DefenseFlat = 1 }, setBonusTin));
			Register(new ItemMetalChestplate("tin", colorTin, new Player.AccumulatedStats() { DefenseFlat = 1 }, setBonusTin));
			Register(new ItemMetalLegs("tin", colorTin, new Player.AccumulatedStats() { DefenseFlat = 0 }, setBonusTin));
			Register(new ItemMetalHelmet("copper", colorCopper, new Player.AccumulatedStats() { DefenseFlat = 1 }, setBonusCopper));
			Register(new ItemMetalChestplate("copper", colorCopper, new Player.AccumulatedStats() { DefenseFlat = 1 }, setBonusCopper));
			Register(new ItemMetalLegs("copper", colorCopper, new Player.AccumulatedStats() { DefenseFlat = 0 }, setBonusCopper));
			Register(new ItemMetalHelmet("bronze", colorBronze, new Player.AccumulatedStats() { DefenseFlat = 2 }, setBonusBronze));
			Register(new ItemMetalChestplate("bronze", colorBronze, new Player.AccumulatedStats() { DefenseFlat = 2 }, setBonusBronze));
			Register(new ItemMetalLegs("bronze", colorBronze, new Player.AccumulatedStats() { DefenseFlat = 2 }, setBonusBronze));
			Register(new ItemTinderbox());
			Register(new ItemBookOfEmber());
			Register(new ItemBoneHelmet());
			Register(new ItemBoneChestplate());
			Register(new ItemBoneLegs());
			Register(new ItemMetalHeart());
			Register(new ItemSkeletonHead());
			Register(new ItemImpEyeball());
			Register(new ItemStonelily());
			Register(new ItemLeatherGloves());
			Register(new ItemMusketBall());
			Register(new ItemMatchlockPistol());
			Register(new ItemStoneBlunderbuss());
			Register(new ItemHandmadeAutoGun());
			Register(new ItemPoisonGun());
			Register(new ItemFlintlockPistol());
			Register(new ItemDebugStructurePaster());
			Register(new ItemLeatherBoots());
			Register(new ItemScrollFind());
			Register(new ItemBookWinds());
			Register(new ItemBookBubble());
			Register(new ItemFeatherRelic());
			Register(new ItemHeart());
			Register(new ItemSuspiciouslyGlowingSkull());
			Register(new ItemRunicBoneSword());
			Register(new ItemBowner());
			Register(new ItemBoneStaff());
			Register(new ItemOssifiedHeart());
			Register(new ItemBoneWhistle());
			Register(new ItemLavaCrystalPickaxe());
			Register(new ItemScrollSonar());
			Register(new ItemPaper());
			Register(new ItemFlaskMagicPotion1());
			Register(new ItemRope());
			Register(new ItemLavaCannon());
			Register(new ItemBookLavaSpout());
			Register(new ItemBookBlank());
			Register(new ItemDebugPlaceBlockWand());
			Register(new ItemRustedSword());
			Register(new ItemOrnamentalSword());
			Register(new ItemCoin("copper", 1, new BrUtility.RectangleF(128, 0, 16, 16)));
            Register(new ItemCoin("bronze", 10, new BrUtility.RectangleF(128 + 16, 0, 16, 16)));
            Register(new ItemCoin("silver", 100, new BrUtility.RectangleF(128 + 32, 0, 16, 16)));
            Register(new ItemCoin("gold", 1000, new BrUtility.RectangleF(128 + 48, 0, 16, 16)));
			Register(new ItemMetalHelmet("oldiron", colorOldIron, new Player.AccumulatedStats() { RangeAtkScale = 0.05f, DefenseFlat = 0 }, setBonusOldIron));
            Register(new ItemMetalChestplate("oldiron", colorOldIron, new Player.AccumulatedStats() { RangeAtkScale = 0.02f, DefenseFlat = 2 }, setBonusOldIron));
            Register(new ItemMetalLegs("oldiron", colorOldIron, new Player.AccumulatedStats() {DefenseFlat = 2 }, setBonusOldIron));
			Register(new ItemSwingTest());
			Register(new ItemLoreIsland1());
			Register(new ItemBread());
			Register(new ItemRodOfShock());
			Register(new ItemCaveRoot());
			Register(new ItemPinkPepper());
			Register(new ItemPotato());
            RegisterItemCubes();
		}

		private void RegisterItemCubes()
		{
			IReadOnlyList<Cube> cubes = Main.Registry.CubeRegistry.GetIterable();
			for (int i = 0; i < Main.Registry.CubeRegistry.Count; i++)
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
