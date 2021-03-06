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
		}
	}
}
