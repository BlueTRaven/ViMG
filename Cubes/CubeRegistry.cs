using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Cubes
{
	public class CubeRegistry : ObjRegistry<Cube>
	{
		public readonly Cube Air = new CubeAir();

		//flags:
		//sparse arrays where flag[cubeid] is the value of the flag, with the intention of low memory size and fast lookups.
		public bool[] noAo;

		protected override void DoRegistration()
		{
			Register(new CubeDirt());
			Register(new CubeGrass());
			Register(new CubeStone());
			Register(new CubeWater());
			Register(new CubeIronOre());
			Register(new CubeTree());
			Register(new CubeGlowDustOre());
			Register(new CubeFurnace());
			Register(new CubeGlowNode());
			Register(new CubeAnvilIron());
			Register(new CubeTinOre());
			Register(new CubeCopperOre());
			Register(new CubeAltarBrick());
			Register(new CubeAncientAltar(false));
			Register(new CubeBrittleBone());
			Register(new CubeSand());
			Register(new CubeChest("wood", 3, 3));
			Register(new CubeObelisk());
			Register(new CubeBundledWood());
			Register(new CubeWood());
			Register(new CubeSapling());
			Register(new CubeFibrousPlant());
			Register(new CubeGlass());
			Register(new CubeCampfire());
			Register(new CubeCaveCompass());
			Register(new CubeLavaCrystal());
			Register(new CubeGeodeStone());
			Register(new CubeCrystal());
			Register(new CubeFlame());
			Register(new CubeChains());
			Register(new CubeStoneBrick());
			Register(new CubeBars());
			for (int i = 0; i < 16; i++)
            {
				int x = i % 8;
				int y = i / 8;
				Register(new CubeDebug("structure_replace_" + i.ToString("00"), new RectangleF(x * 16, 976 + y * 16, 16, 16), Color.White, 1));
            }
			Register(new CubeShrine("shrine_shimu", new RectangleF(128, 64, 16, 16), "shimu_blessing", "Shrine to Shimu"));
			Register(new CubeShrine("shrine_irat", new RectangleF(144, 64, 16, 16), "irat_blessing", "Shrine to Irat"));
			Register(new CubeShrine("shrine_adrath", new RectangleF(160, 64, 16, 16), "adrath_blessing", "Shrine to Adrath"));
			Register(new CubeShrine("shrine_akkat", new RectangleF(176, 64, 16, 16), "akkat_blessing", "Shrine to Akkat"));
			Register(new CubeShrine("shrine_gidamu", new RectangleF(192, 64, 16, 16), "gidamu_blessing", "Shrine to Gidamu"));
			Register(new CubeShrine("shrine_arat", new RectangleF(208, 64, 16, 16), "arat_blessing", "Shrine to Arat"));
			Register(new CubeAncientAltar(true));
			Register(new CubeAzureFlower());
			Register(new CubeMushroomOrangeTop());
			Register(new CubeMushroomStem());
			Register(new CubeOrangeCoveredStone());
			Register(new CubeRope());
			Register(new CubeMushroomOrangeSmall());
			Register(new CubeMushroomPurpleTop());
			Register(new CubeMushroomPurpleSmall());
			Register(new CubePurpleCoveredStone());
			Register(new CubeBonfire());
			Register(new CubeBonepile());
			Register(new CubeChainLight());
			Register(new CubeWoodPlatform());
			Register(new CubeCryptStone());
			Register(new CubeCryptCobbles());
			Register(new CubeSeaStone());
			Register(new CubeSeaStuddedStone());
			Register(new CubeDoor());
			Register(new CubeThorn());
		}

        protected override void PostRegistration()
        {
			noAo = new bool[Count + 1];
			noAo[0] = true;

			for (int i = 1; i <= Count; i++)
            {
				Get(i).SetId((ushort)(i));
			}

			base.PostRegistration();
        }

        protected override void Register(Cube obj)
		{
			base.Register(obj);
		}
	}
}
