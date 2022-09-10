using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public static class Util
    {
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
