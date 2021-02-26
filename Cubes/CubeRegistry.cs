using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Cubes
{
	public class CubeRegistry
	{
		private List<Cube> registry = new List<Cube>();
		public int Count => registry.Count;

		public void RegisterCubes()
		{
			registry.Add(new Cube(new RectangleF(0, 0, 16, 16), Color.White, 2));
			registry.Add(new CubeGrass());
			registry.Add(new Cube(new RectangleF(16, 0, 16, 16), Color.White, 5));
			registry.Add(new Cube(new RectangleF(0, 16, 16, 16), Color.White, 1));
		}

		public Cube Get(int index)
		{
			if (index == 0)
				return null;
			return registry[index - 1];
		}
	}
}
