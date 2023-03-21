using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using ViMG.Items;

namespace ViMG
{
	public static class SaveHelper
	{
		//Saves a struct.
		//I don't really recommend using this method. Manually saving/loading is a better approach
		//since you can manually handle error cases. For instance, if you add a new float in the middle of a struct,
		//LoadStruct will behave weird.
		public static void SaveStruct<T>(List<byte> data, T obj) where T : struct
		{
            var len = Marshal.SizeOf<T>();
            byte[] bytes = new byte[len];

            nint ptr = Marshal.AllocHGlobal(len);
            Marshal.StructureToPtr(obj, ptr, false);
            Marshal.Copy(ptr, bytes, 0, len);
            Marshal.FreeHGlobal(ptr);

            data.AddRange(bytes);
        }

		public static void SaveBool(List<byte> data, bool b)
        {
			data.Add((byte)(b ? 0 : 1));
        }

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

		public static void SaveUInt16(List<byte> data, int i)
        {
			byte first = (byte)(i & 0b11111111);
			byte second = (byte)((i & (0b11111111 << 8)) >> 8);

			data.Add(first);
			data.Add(second);
		}

		public static void SaveUInt64(List<byte> data, ulong i)
        {
			SaveInt32(data, (int)(i & (ulong)(4294967295)));
			SaveInt32(data, (int)((i & (ulong)(4294967295 << 32)) >> 32));
		}

		public static void SaveFloat32(List<byte> data, float f)
		{
			byte[] bytes = BitConverter.GetBytes(f);

			data.Add(bytes[0]);
			data.Add(bytes[1]);
			data.Add(bytes[2]);
			data.Add(bytes[3]);
		}

		public static void SaveVector2(List<byte> data, Vector2 vec)
		{
			SaveFloat32(data, vec.X);
			SaveFloat32(data, vec.Y);
		}

		public static void SaveVector3(List<byte> data, Vector3 vec)
		{
			SaveFloat32(data, vec.X);
			SaveFloat32(data, vec.Y);
			SaveFloat32(data, vec.Z);
		}

		public static void SaveVector4(List<byte> data, Vector4 vec)
		{
			SaveFloat32(data, vec.X);
			SaveFloat32(data, vec.Y);
			SaveFloat32(data, vec.Z);
			SaveFloat32(data, vec.W);
		}

		//Saves bytes "flat" (without overhead, as raw bytes - unnassociated with any array) from data2 into data1.
		//This doesn't need a load variation.
		public static void SaveBytesFlat(List<byte> data1, List<byte> data2)
        {
			for (int i = 0; i < data2.Count; i++)
            {
				data1.Add(data2[i]);
            }
        }

		public static void SaveBytesFlat(List<byte> data1, byte[] data2) 
		{
			for (int i = 0; i < data2.Length; i++)
			{
				data1.Add(data2[i]);
			}
		}

		public static void SaveBytesFlat(List<byte> data1, List<byte> data2, int index, int length)
		{
			for (int i = index; i < index + length; i++)
			{
				data1.Add(data2[i]);
			}
		}

		public static void SaveBytesFlat(List<byte> data1, byte[] data2, int index, int length)
        {
			for (int i = index; i < length; i++)
			{
				data1.Add(data2[i]);
			}
		}

		//Must be in cube space
		public static void SaveCubePosition(List<byte> data, CubePosition position)
		{
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

		//Note that this assumes the WHOLE of `data` contains the data for the struct.
		public static T LoadStruct<T>(byte[] data) where T : struct
		{
            var len = Marshal.SizeOf<T>();

            var ptr = Marshal.AllocHGlobal(len);
            Marshal.Copy(data, 0, ptr, len);
            T obj = Marshal.PtrToStructure<T>(ptr);
            Marshal.FreeHGlobal(ptr);

			return obj;
        }

		public static bool LoadBool(byte[] data, ref int index)
        {
			byte b = data[index++];
			return b >= 1;
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

		public static ushort LoadUInt16(byte[] data, ref int index)
        {
			int first = data[index++];
			int second = data[index++] << 8;

			return (ushort)(first | second);
		}

		public static ulong LoadUInt64(byte[] data, ref int index)
		{
			unchecked
			{
				ulong first = (ulong)LoadInt32(data, ref index);
				ulong second = (ulong)LoadInt32(data, ref index) << 32;
				return first | second;
			}
		}

		public static float LoadFloat32(byte[] data, ref int index)
		{
			float f = BitConverter.ToSingle(data, index);
			index += 4;

			return f;
		}

		public static byte[] LoadBytes(byte[] data, int length, ref int index)
        {
			int end = index + length;
			byte[] rval = data[index..end];
			index = end;

			return rval;
        }

		public static Vector2 LoadVector2(byte[] data, ref int index)
		{
			float x = LoadFloat32(data, ref index);
			float y = LoadFloat32(data, ref index);

			return new Vector2(x, y);
		}

		public static Vector3 LoadVector3(byte[] data, ref int index)
		{
			float x = LoadFloat32(data, ref index);
			float y = LoadFloat32(data, ref index);
			float z = LoadFloat32(data, ref index);

			return new Vector3(x, y, z);
		}

		public static Vector4 LoadVector4(byte[] data, ref int index)
		{
			float x = LoadFloat32(data, ref index);
			float y = LoadFloat32(data, ref index);
			float z = LoadFloat32(data, ref index);
			float w = LoadFloat32(data, ref index);

			return new Vector4(x, y, z, w);
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
