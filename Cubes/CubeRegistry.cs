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
			Register(new CubeAncientAltar());
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
