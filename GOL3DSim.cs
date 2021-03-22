using System;
using System.Collections.Generic;
using System.Text;
using BrUtility;

namespace ViMG
{
	public class GOL3DSim
	{
		private int width, height, depth;
		private readonly int iterations;
		private readonly float chanceToStartAlive;
		private readonly int deathLimit;
		private readonly int birthLimit;
		private bool[][][] sim;

		public GOL3DSim(Random random, int width, int height, int depth, int iterations, float chanceToStartAlive, int deathLimit, int birthLimit)
		{
			this.width = width;
			this.height = height;
			this.depth = depth;
			this.iterations = iterations;
			this.chanceToStartAlive = chanceToStartAlive;
			this.deathLimit = deathLimit;
			this.birthLimit = birthLimit;

			sim = new bool[width][][];//[height][depth];//new bool[width * height * depth];

			for (int x = 0; x < width; x++)
			{
				sim[x] = new bool[height][];

				for (int y = 0; y < height; y++)
				{
					sim[x][y] = new bool[depth];

					for (int z = 0; z < depth; z++)
					{
						if (random.NextFloat() < chanceToStartAlive)
						{
							Set(sim, x, y, z, true);
						}
					}
				}
			}
		}

		public void DoSim()
		{
			for (int i = 0; i < iterations; i++)
			{
				SimIteration();
			}
		}

		private void SimIteration()
		{
			//bool[] currentSim = new bool[width * height * depth];
			bool[][][] currentSim = new bool[width][][];

			for (int x = 0; x < width; x++)
			{
				currentSim[x] = new bool[height][];

				for (int y = 0; y < height; y++)
				{
					currentSim[x][y] = new bool[depth];

					for (int z = 0; z < depth; z++)
					{
						int num = CountNeighbors(x, y, z);

						if (Get(x, y, z))
						{
							if (num < deathLimit)
							{
								Set(currentSim, x, y, z, false);
							}
							else
							{
								Set(currentSim, x, y, z, true);
							}
						}
						else
						{
							if (num > birthLimit)
							{
								Set(currentSim, x, y, z, true);
							}
							else
							{
								Set(currentSim, x, y, z, false);
							}
						}
					}
				}
			}

			sim = currentSim;
		}

		private int CountNeighbors(int xs, int ys, int zs)
		{
			int num = 0;
			for (int x = -1; x <= 1; x++)
			{
				for (int y = -1; y <= 1; y++)
				{
					for (int z = -1; z <= 1; z++)
					{
						if (x == 0 && y == 0 && z == 0)
							continue;

						int cx = xs + x;
						int cy = ys + y;
						int cz = zs + z;

						if (cx < 0 || cx >= width || cy < 0 || cy >= height || cz < 0 || cz >= depth)
						{
							num++;
							continue;
						}

						if (Get(cx, cy, cz))
							num++;
					}
				}
			}

			return num;
		}

		public bool Get(int x, int y, int z)
		{
			return sim[x][y][z];
		}

		private void Set(bool[][][] sim, int x, int y, int z, bool value)
		{
			int index = x + height * (y + width * z);

			sim[x][y][z] = value;
		}
	}
}
