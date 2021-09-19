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

		public override void RegisterAll()
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
		}

		protected override void Register(Cube obj)
		{
			obj.SetId((ushort)(Count + 1));
			base.Register(obj);
		}
	}
}
