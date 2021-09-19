using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG
{
	public static class SaveHelper
	{
		public static void SaveString(List<byte> data, string str)
		{
			SaveInt32(data, str.Length);

			for (int i = 0; i < str.Length; i++)
				SaveChar(data, str[i]);
		}

		public static void SaveChar(List<byte> data, char chr)
		{
			byte first = (byte)(chr & 0b11111111);
			byte second = (byte)((chr & (0b11111111 << 8)) >> 8);

			data.Add(first);
			data.Add(second);
		}

		public static void SaveInt32(List<byte> data, int i)
		{
			byte first = (byte)(i & 0b11111111);
			byte second = (byte)((i & (0b11111111 << 8)) >> 8);
			byte third = (byte)((i & (0b11111111 << 16)) >> 16);
			byte fourth = (byte)((i & (0b11111111 << 24)) >> 24);

			data.Add(first);
			data.Add(second);
			data.Add(third);
			data.Add(fourth);
		}

		public static void SaveFloat32(List<byte> data, float f)
		{
			byte[] bytes = BitConverter.GetBytes(f);

			data.Add(bytes[0]);
			data.Add(bytes[1]);
			data.Add(bytes[2]);
			data.Add(bytes[3]);
		}

		public static void SaveVector3(List<byte> data, Vector3 vec)
		{
			SaveFloat32(data, vec.X);
			SaveFloat32(data, vec.Y);
			SaveFloat32(data, vec.Z);
		}

		public static void SaveCubePosition(List<byte> data, CubePosition position, Chunk chunk = null)
		{
			if (position.Coord == CubePosition.CoordinateSpace.ChunkSpace && chunk != null)
				position.InCubeSpace(chunk);

			SaveInt32(data, position.X);
			SaveInt32(data, position.Y);
			SaveInt32(data, position.Z);
		}

		public static void SaveItemInstance(List<byte> data, ItemInstance item)
		{
			SaveString(data, item.item.Identifier);
			SaveInt32(data, item.num);
			SaveInt32(data, item.damage);
		}

		public static string LoadString(byte[] data, ref int index)
		{
			int len = LoadInt32(data, ref index);

			char[] chars = new char[len];
			for (int i = 0; i < len; i++)
			{
				char c = LoadChar(data, ref index);

				chars[i] = c;
			}

			return new string(chars);
		}

		public static char LoadChar(byte[] data, ref int index)
		{
			int first = data[index++];
			int second = data[index++] << 8;

			return (char)(first | second);
		}

		public static int LoadInt32(byte[] data, ref int index)
		{
			int first = data[index++];
			int second = data[index++] << 8;
			int third = data[index++] << 16;
			int fourth = data[index++] << 24;

			return first | second | third | fourth;
		}

		public static float LoadFloat32(byte[] data, ref int index)
		{
			float f = BitConverter.ToSingle(data, index);
			index += 4;

			return f;
		}

		public static Vector3 LoadVector3(byte[] data, ref int index)
		{
			float x = LoadFloat32(data, ref index);
			float y = LoadFloat32(data, ref index);
			float z = LoadFloat32(data, ref index);

			return new Vector3(x, y, z);
		}

		public static CubePosition LoadCubePosition(byte[] data, ref int index)
		{
			CubePosition position = new CubePosition(0, 0, 0, CubePosition.CoordinateSpace.CubeSpace);

			position.X = LoadInt32(data, ref index);
			position.Y = LoadInt32(data, ref index);
			position.Z = LoadInt32(data, ref index);

			return position;
		}

		public static ItemInstance LoadItemInstance(byte[] data, ref int index)
		{
			string identifier = LoadString(data, ref index);
			Item item = Main.Registry.ItemRegistry.Get(identifier);

			int num = LoadInt32(data, ref index);
			int damage = LoadInt32(data, ref index);

			return new ItemInstance(item, num, damage);
		}
	}
}
