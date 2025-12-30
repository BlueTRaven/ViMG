using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.IMGUIImpl;

namespace Engine.Common
{
    public enum PalettizeType
    {
        AllOneId,
        _1bit,
        _2bit,
        _4bit,
        _8bit,
        _16bit,
    }

    public struct PalettizedChunk
    {
        public ChunkPosition position;
        public PalettizeType type;
        public ushort[] palette;
        public byte[]? data;

        public void Save(Stream stream)
        {
            List<byte> bytes = new List<byte>();
            SaveHelper.SaveInt32(bytes, (int)type);
            SaveHelper.SaveInt32(bytes, palette.Length);
            for (int i = 0; i < palette.Length; i++)
                SaveHelper.SaveUInt16(bytes, palette[i]);

            if (type != PalettizeType.AllOneId)
            {
                SaveHelper.SaveInt32(bytes, data.Length);
                SaveHelper.SaveBytesFlat(bytes, data);
            }

            stream.Write(bytes.ToArray());
        }

        public void Load(Span<byte> bytes)
        {
            int offset = 0;
            type = (PalettizeType)SaveHelper.LoadInt32(bytes, ref offset);
            Debug.Assert(type <= PalettizeType._8bit && type >= 0);
            int palLen = SaveHelper.LoadInt32(bytes, ref offset);
            Debug.Assert(palLen <= ExpectedPaletteMax(type));
            palette = new ushort[palLen];
            for (int i = 0; i < palLen; i++)
                palette[i] = SaveHelper.LoadUInt16(bytes, ref offset);

            if (type != PalettizeType.AllOneId)
            {
                int dataLen = SaveHelper.LoadInt32(bytes, ref offset);
                Debug.Assert(dataLen == ExpectedLen(type));
                data = SaveHelper.LoadBytes(bytes, dataLen, ref offset).ToArray();
            }
        }

        public ushort GetId(CubePosition position)
        {
            Util.ThreeDToOneD(new ValuePoint3D(position), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);

            if (type == PalettizeType.AllOneId)
            {
                return palette[0];
            }
            else
            {
                byte bitmask = 0;
                int bits = 0;
                if (type == PalettizeType._1bit)
                {
                    bitmask = 0b1;
                    bits = 1;
                }
                else if (type == PalettizeType._2bit)
                {
                    bitmask = 0b11;
                    bits = 2;
                }
                else if (type == PalettizeType._4bit)
                {
                    bitmask = 0b1111;
                    bits = 4;
                }
                else if (type == PalettizeType._8bit)
                {
                    bitmask = 255;
                    bits = 8;
                }

                int atByte = (int)(i * ((float)bits / 8f + 1f));
                int atBit = (i * bits) % 8;

                byte index = (byte)((data[atBit] >> atBit) & bitmask);

                return palette[index];
            }

            return 0;
        }

        public static int ExpectedPaletteMax(PalettizeType t)
        {
            return t switch
            {
                PalettizeType.AllOneId => 1,
                PalettizeType._1bit => 2,
                PalettizeType._2bit => 4,
                PalettizeType._4bit => 16,
                PalettizeType._8bit => 256,
                PalettizeType._16bit => throw new NotImplementedException(),
            };
        }

        public static int ExpectedLen(PalettizeType t)
        {
            return t switch
            {
                PalettizeType.AllOneId => 3,
                PalettizeType._1bit => 512,
                PalettizeType._2bit => 1024,
                PalettizeType._4bit => 2048,
                PalettizeType._8bit => 4096,
                _ => 0,
            };
        }
        public static PalettizedChunk Palettize(ChunkPosition chunkPosition, Span<ushort> ids)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

            var chunk = new PalettizedChunk
            {
                type = PalettizeType.AllOneId,
                position = chunkPosition,
            };

            IMGUIConsole.Assert(ids.Length == Chunk.NUM_CUBES_IN_CHUNK);

            //Span<ushort> ids = stackalloc ushort[Chunk.NUM_CUBES_IN_CHUNK];
            //GetIdsForChunk(chunkPosition, ids);
            Span<int> usedIds = stackalloc int[Main.Registry.CubeRegistry.Count];
            for (int i = 0; i < usedIds.Length; i++)
                usedIds[i] = -1;
            Span<ushort> uniqueIds = stackalloc ushort[Chunk.NUM_CUBES_IN_CHUNK];
            Span<ushort> palettizedIds = stackalloc ushort[Chunk.NUM_CUBES_IN_CHUNK];

            ushort uniqueIdsLen = 0;

            for (int i = 0; i < ids.Length; i++)
            {
                ushort id = ids[i];
                if (usedIds[id] < 0)
                {
                    usedIds[id] = uniqueIdsLen;
                    uniqueIds[uniqueIdsLen] = id;
                    uniqueIdsLen += 1;
                }
                IMGUIConsole.Assert(usedIds[id] >= 0);
                palettizedIds[i] = (ushort)usedIds[id];
            }

            chunk.palette = uniqueIds[0..uniqueIdsLen].ToArray();
            if (uniqueIdsLen == 1)
            {
                return chunk;
            }
            else
            {
                int bitmask = 0;
                int bits = 0;
                List<byte> bytes = new List<byte>();
                if (uniqueIdsLen <= 2)
                {
                    bitmask = 0b1;
                    bits = 1;
                    chunk.type = PalettizeType._1bit;
                }
                else if (uniqueIdsLen <= 4)
                {
                    bitmask = 0b11;
                    bits = 2;
                    chunk.type = PalettizeType._2bit;

                }
                else if (uniqueIdsLen <= 16)
                {
                    bitmask = 0b1111;
                    bits = 4;
                    chunk.type = PalettizeType._4bit;
                }
                else if (uniqueIdsLen <= 256)
                {
                    bitmask = 255;
                    bits = 8;
                    chunk.type = PalettizeType._8bit;
                }
                else
                {
                    IMGUIConsole.Assert(false, "Unimplemented");
                }

                int numfit = sizeof(byte) * 8 / bits;
                byte currentByte = 0;
                int placeInByte = 0;

                for (int i = 0; i < ids.Length; i++)
                {
                    IMGUIConsole.Assert(placeInByte < 8);
                    IMGUIConsole.Assert(palettizedIds[i] <= bitmask);
                    currentByte |= (byte)((palettizedIds[i] & bitmask) << placeInByte);

                    placeInByte += bits;
                    if (placeInByte >= 8)
                    {
                        placeInByte = 0;
                        bytes.Add(currentByte);
                        currentByte = 0;
                    }
                }

                chunk.data = bytes.ToArray();
            }

            //Console.WriteLine("Chunk {0} Was: {1}", chunk.position, chunk.type.ToString());
            //Console.WriteLine("Size: {0} in palette, {1} bytes", chunk.palette.Length, chunk.data.Length);
            //Console.WriteLine("Palette: ");
            //foreach (ushort id in chunk.palette)
            //{
            //    Console.WriteLine(Main.Registry.CubeRegistry.GetOrDefault(id, Main.Registry.CubeRegistry.Air));
            //}

            return chunk;
        }

        public static ushort[] Depaletteize(PalettizedChunk chunk)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

            var ids = new ushort[Chunk.NUM_CUBES_IN_CHUNK];
            if (chunk.type == PalettizeType.AllOneId)
            {
                Array.Fill(ids, chunk.palette[0]);
            }
            else
            {
                byte bitmask = 0;
                int bits = 0;
                if (chunk.type == PalettizeType._1bit)
                {
                    bitmask = 0b1;
                    bits = 1;
                }
                else if (chunk.type == PalettizeType._2bit)
                {
                    bitmask = 0b11;
                    bits = 2;
                }
                else if (chunk.type == PalettizeType._4bit)
                {
                    bitmask = 0b1111;
                    bits = 4;
                }
                else if (chunk.type == PalettizeType._8bit)
                {
                    bitmask = 255;
                    bits = 8;
                }
                else
                {
                    IMGUIConsole.Assert(false, "Unimplemented");
                }

                int numfit = sizeof(byte) * 8 / bits;
                int currentByteIndex = 0;
                byte currentByte = chunk.data[currentByteIndex];
                currentByteIndex += 1;
                int placeInByte = 0;

                for (int i = 0; i < ids.Length; i++)
                {
                    IMGUIConsole.Assert((currentByteIndex - 1) == i / numfit);
                    IMGUIConsole.Assert(placeInByte < 8);

                    ushort paletteId = (ushort)((currentByte >> placeInByte) & bitmask);
                    IMGUIConsole.Assert(paletteId <= bitmask);

                    ids[i] = chunk.palette[paletteId];
                    placeInByte += bits;
                    if (placeInByte >= 8 && i != ids.Length - 1)
                    {
                        placeInByte = 0;
                        currentByte = chunk.data[currentByteIndex];
                        currentByteIndex += 1;
                    }
                }
            }

            return ids;
        }
    }
}
