using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG
{
    public static class Util
    {
		public enum MineTier
        {
			Weak,
			Brittle,
        }

		public static string RangeToString(Player.DamageType damageType, float range)
        {
			if (damageType == Player.DamageType.Melee)
			{
				if (range <= Cube.CUBE_SCALE / 2f)
					return "Teeny Tiny";
				else if (range <= Cube.CUBE_SCALE)
					return "Short";
				else if (range <= Cube.CUBE_SCALE * 2.5f)
					return "Medium";
				else if (range <= Cube.CUBE_SCALE * 3.5f)
					return "Ordinary";
				else if (range <= Cube.CUBE_SCALE * 5f)
					return "Long";
				else if (range >= Cube.CUBE_SCALE * 5f)
					return "Very Long";
			}

			return "UNKNOWN???";
        }

		public static string CooldownToString(float cooldownTime)
		{
			if (cooldownTime <= 0.125f)
				return "Blisteringly Fast";
			else if (cooldownTime <= 0.25f)
				return "Very Fast";
			else if (cooldownTime < 0.5)
				return "Fast";

			if (cooldownTime >= 2.5f)
				return "Snail";
			else if (cooldownTime >= 2f)
				return "Very Slow";
			else if (cooldownTime >= 1.5f)
				return "Sluggish";
			else if (cooldownTime >= 1f)
				return "Slow";
			else if (cooldownTime >= 0.5f)
				return "Ordinary";

			return "UNKNOWN???";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void OneDToThreeD(int i, out ValuePoint3D point)
		{
			int x = i % Chunk.CHUNK_SIZE;
			int y = (i / Chunk.CHUNK_SIZE) % Chunk.CHUNK_SIZE;
			int z = i / (Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE);

			point = new ValuePoint3D(x, y, z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void OneDToThreeD(int i, ValuePoint3D size, out ValuePoint3D point)
        {
			int x = i % size.x;
			int y = (i / size.x) % size.y;
			int z = i / (size.y * size.x);

			point = new ValuePoint3D(x, y, z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThreeDToOneD(ValuePoint3D point, out int i)
		{
			i = point.x + Chunk.CHUNK_SIZE * (point.y + Chunk.CHUNK_SIZE * point.z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThreeDToOneD(ValuePoint3D point, ValuePoint3D size, out int i)
		{
			i = point.x + size.x * (point.y + size.y * point.z);
		}
	}
}
