using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG
{
    public class ChunkManagerIO : WorldIO
    {
		public const string FILE_NAME_CHUNK_OLD = "chunks_";
		public const string FILE_NAME_CHUNK = "chunks";
		public const string EXT_CHUNK = ".vis";

		private const long SIZEOF_HEADER = sizeof(int) * 4;
		private const long SIZEOF_CHUNK = (sizeof(ushort) * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE);

		private const int VERSION = 3;
		private const int MIN_VERSION = 1;

		public int Version;

		//private readonly ChunkManager manager;
		private readonly string managerName;
        private readonly int sizeInChunks;
        private int layer;
		private readonly long numChunks;

		private bool loaded;
		private byte[] allBytes;

		//private const int NUM_CHUNKS_TO_KEEP_IN_MEMORY = 1726;
		//private const long SIZEOF_CHUNKS_IN_MEMORY = NUM_CHUNKS_TO_KEEP_IN_MEMORY * SIZEOF_CHUNK;
		//private int offset;
		//private FileStream fs;

		public ChunkManagerIO(int sizeInChunks, string chunkManagerName, int layer)
        {
            //this.manager = manager;
			this.managerName = chunkManagerName;
            this.layer = layer;
            this.sizeInChunks = sizeInChunks;
			numChunks = sizeInChunks * sizeInChunks * sizeInChunks;
			allBytes = new byte[numChunks * SIZEOF_CHUNK];
        }

		public void Initialize()
        {

        }

		public byte[] GetBytes()
        {
			/*CubePosition givenPosition = new CubePosition();

			int givenOffset = GetCubeOffset(givenPosition);

			if (offset < givenOffset || (givenOffset - offset) >= SIZEOF_CHUNKS_IN_MEMORY)
            {
				offset = givenOffset;
				fs.Seek(offset, SeekOrigin.Begin);
				fs.Read(allBytes, offset + (int)SIZEOF_HEADER, (int)SIZEOF_CHUNKS_IN_MEMORY);
            }*/

			return allBytes;
        }

		public int GetChunkOffset(CubePosition position)
        {
			int chx = position.X / Chunk.CHUNK_SIZE;
			int chy = position.Y / Chunk.CHUNK_SIZE;
			int chz = position.Z / Chunk.CHUNK_SIZE;
			//use it to find offset in byte array
			int chunkOffset = chx + sizeInChunks * (chy + sizeInChunks * chz);
			chunkOffset *= Chunk.NUM_CUBES_IN_CHUNK;

			return chunkOffset;
		}

		public int GetChunkOffset(ChunkPosition position)
        {
			int chunkOffset = position.X + sizeInChunks * (position.Y + sizeInChunks * position.Z);
			chunkOffset *= Chunk.NUM_CUBES_IN_CHUNK;

			return chunkOffset;
		}

		public static int GetCubeOffset(CubePosition position, int sizeInChunks = 32)
        {
			//NOTE: we can't just index directly into bytes (as a ushort)
			//This is because we store cube ids weirdly. We do not store them flat, one after another; instead, we store them as a chunk, then as another chunk, etc.
			//This may introduce problems here, but I don't think I want to change that behavior
			//as it may help later down the line of we want to, say, introduce streaming. Streaming individual cubes?
			//Pretty useless. Chunks, however, are a much more useful streamable object.

			//Get chunk position...
			int chx = position.X / Chunk.CHUNK_SIZE;
			int chy = position.Y / Chunk.CHUNK_SIZE;
			int chz = position.Z / Chunk.CHUNK_SIZE;
			//use it to find offset in byte array
			int chunkOffset = chx + sizeInChunks * (chy + sizeInChunks * chz);
			chunkOffset *= Chunk.NUM_CUBES_IN_CHUNK;

			//Get chunk relative cube position...
			//https://stackoverflow.com/questions/11040646/faster-modulus-in-c-c
			//Faster mod when denominator is a power of 2.
			//NOTE: if Chunk.CHUNK_SIZE changes and no longer is a power of two, THIS WILL BREAK EVERYTHING!
			int csx = position.X & (Chunk.CHUNK_SIZE - 1);
			int csy = position.Y & (Chunk.CHUNK_SIZE - 1);
			int csz = position.Z & (Chunk.CHUNK_SIZE - 1);

			int cubeOffset = csx + Chunk.CHUNK_SIZE * (csy + Chunk.CHUNK_SIZE * csz);
			cubeOffset += chunkOffset;

			return cubeOffset;
		}

		//Saves the contents of allBytes to disk.
		public void Save(string folderName)
        {
			if (!Directory.Exists(SAVE_FOLDER + folderName))
				Directory.CreateDirectory(SAVE_FOLDER + folderName);

			//FileStream is probably unnecessary since we're saving the everything all at once
			using (FileStream fs = new FileStream(GetSaveName(folderName), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
			{
				SaveToStream(fs);
			}

			bool anyNotZero = false;

			for (int i = 0; i < allBytes.Length; i++)
			{
				if (allBytes[i] != 0)
				{
					anyNotZero = true;
					break;
				}
			}
		}

		public void SaveToStream(Stream stream)
		{
            stream.Write(BitConverter.GetBytes(VERSION));
            stream.Write(BitConverter.GetBytes(layer));

            //Write unused remaining header bytes
            long remainingBytes = SIZEOF_HEADER - stream.Position;
            stream.Write(new byte[remainingBytes]);

            stream.Write(allBytes);

			Console.WriteLine("Wrote {0} bytes", stream.Length);
        }

		public LoadError Load(string folderName)
		{
			if (!File.Exists(GetLoadName(folderName)))
				return LoadError.FileDoesntExist;
				
			using (FileStream fs = new FileStream(GetLoadName(folderName), FileMode.Open, FileAccess.Read, FileShare.None))
			{
				var result = LoadFromStream(fs);

				if (result != LoadError.Success)
					return result;
			}

			loaded = true;

			bool anyNotZero = false;

			for (int i = 0; i < allBytes.Length; i++)
            {
				if (allBytes[i] != 0)
                {
					anyNotZero = true;
					break;
                }
            }

			if (!anyNotZero)
			{
				OtherError = "File was empty. No cube data loaded.";
				return LoadError.Other;
			}

			return LoadError.Success;
		}

		public LoadError LoadFromStream(Stream stream)
		{
			Console.WriteLine("Reading {0} bytes", stream.Length);

            using (BinaryReader br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                Version = br.ReadInt32();

                if (Version >= 2)
                {
                    var loadedLayer = br.ReadInt32();
                    if (loadedLayer != layer)
                    {
                        OtherError = string.Format("Tried to load a chunk file as layer {0}, but it actually belongs to layer {1}!", layer, loadedLayer);
                        return LoadError.Other;
                    }
                }

                if (Version < MIN_VERSION)
                    return LoadError.InvalidVersion;
                else
                {
                    //Discard the rest of the buffer.
                    int remainingBytes = (int)(SIZEOF_HEADER - stream.Position);
                    br.Read(new byte[remainingBytes], 0, remainingBytes);
                }
            }

            stream.Read(allBytes, 0, (int)(numChunks * SIZEOF_CHUNK));

			return LoadError.Success;
        }

		//Deserializes a chunk from the local byte stream into the world. This does not load anything from the disk! If nothing has been loaded yet, this will error!
		/*public void DeserializeChunk(World world, ChunkPosition pos)
        {
			Util.ThreeDToOneD(new ValuePoint3D(pos.X, pos.Y, pos.Z), new ValuePoint3D(manager.sizeInChunksXZ, manager.sizeInChunksXZ, manager.sizeInChunksXZ), out int i);

			DeserializeChunk(world, pos, i);
        }

        private void DeserializeChunk(World world, ChunkPosition pos, int i)
		{
			if (!loaded)
				throw new Exception("Attempted to deserialize when nothing has been loaded. Call Load first!");

			Chunk chunk = manager.GetChunk(pos);
			ChunkData data = chunk?.GetData();

			if (data == null)
			{
				data = manager.ChunkDatas.Get();
				chunk.SetData(data);
			}

			int chunkOffset = (int)SIZEOF_CHUNK * i;

			byte[] buffer = allBytes[chunkOffset..(chunkOffset + (int)SIZEOF_CHUNK)];

			ushort[] allCubes = chunk.GetData().GetAll();
			for (int j = 0; j < Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE; j++)
			{
				const int NUM_BYTES_PER_CUBE = sizeof(ushort);
				int offset = j * NUM_BYTES_PER_CUBE;

				byte a = buffer[offset + 0];
				byte b = buffer[offset + 1];

				//bitwise operators are not defined for ushort, so we're forced to cast... fun.
				int id = 0;
				id |= b << 8;
				id |= a << 0;

				Cube c = Main.Registry.CubeRegistry.Get(id);

				if (c != null)
				{
					Util.OneDToThreeD(j, new ValuePoint3D(Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE), out ValuePoint3D point);
					c.OnLoaded(world, new CubePosition(point.x, point.y, point.z, CubePosition.CoordinateSpace.ChunkSpace).InCubeSpace(chunk));
				}
				else if (id != 0)	//0 is null but obviously isn't included as an invalid cube
				{
					Util.OneDToThreeD(j, new ValuePoint3D(Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE), out ValuePoint3D point);
					Console.WriteLine("Invalid cube ID in chunk x {0} y {1} z {2}, pos x {3} y {4} z {5}", pos.X, pos.Y, pos.Z, point.x, point.y, point.z);
				}

				allCubes[j] = (ushort)id;
			}

			chunk.Initialize(world);
			data.GenStep = ChunkData.GenerationStep.Done;

			manager.MarkDirty(pos, false);
		}*/

		private string GetSaveName(string folderName)
        {
			return SAVE_FOLDER + folderName + "/" + FILE_NAME_CHUNK + layer + EXT_CHUNK;
		}

		private string GetLoadName(string folderName)
        {
			if (!File.Exists(SAVE_FOLDER + folderName + "/" + FILE_NAME_CHUNK + layer + EXT_CHUNK))
				return SAVE_FOLDER + folderName + "/" + FILE_NAME_CHUNK_OLD + managerName + EXT_CHUNK;
			else return SAVE_FOLDER + folderName + "/" + FILE_NAME_CHUNK + layer + EXT_CHUNK;
		}

		public override bool HandleError(LoadError error, string folderName)
        {
            switch (error)
            {
                case LoadError.InvalidVersion:
                    Console.WriteLine("Chunk file could not be loaded. The current file version ({0}) is not supported.", Version);
                    return true;
                case LoadError.FileDoesntExist:
                    Console.WriteLine("Chunk file does not exist.", GetLoadName(folderName));
                    return true;
                case LoadError.Other:
                    Console.WriteLine(OtherError);
                    return true;
                case LoadError.Success:
                    return false;
                default:
                    return true;
            }
        }
	}
}
