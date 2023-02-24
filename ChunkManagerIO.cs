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
		public const string FILE_NAME_CHUNK = "chunks_";
		public const string EXT_CHUNK = ".vis";

		private const long SIZEOF_HEADER = sizeof(int) * 4;
		private const long SIZEOF_CHUNK = (sizeof(ushort) * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE);

		private const int VERSION = 2;
		private const int MIN_VERSION = 1;

		public int Version;

		//private readonly ChunkManager manager;
		private readonly string managerName;

		private readonly int sizeInChunks;
		private readonly long numChunks;

		private bool loaded;
		private byte[] allBytes;

		//private const int NUM_CHUNKS_TO_KEEP_IN_MEMORY = 1726;
		//private const long SIZEOF_CHUNKS_IN_MEMORY = NUM_CHUNKS_TO_KEEP_IN_MEMORY * SIZEOF_CHUNK;
		//private int offset;
		//private FileStream fs;

		public ChunkManagerIO(int sizeInChunks, string chunkManagerName)
        {
            //this.manager = manager;
			this.managerName = chunkManagerName;

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

		public int GetCubeOffset(CubePosition position)
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
			using (FileStream fs = new FileStream(GetFullName(folderName), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
			{
				fs.Write(BitConverter.GetBytes(VERSION));
				fs.Write(BitConverter.GetBytes(0));

				//Write unused remaining header bytes
				long remainingBytes = SIZEOF_HEADER - fs.Position;
				fs.Write(new byte[remainingBytes]);

				fs.Write(allBytes);
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

		public LoadError Load(string folderName)
		{
			using (FileStream fs = new FileStream(GetFullName(folderName), FileMode.Open, FileAccess.Read, FileShare.None))
			{
				using (BinaryReader br = new BinaryReader(fs, Encoding.ASCII, true))
				{
					Version = br.ReadInt32();

					int layers = 1;
					if (Version >= 2)
						layers = br.ReadInt32();

					if (Version < MIN_VERSION)
						return LoadError.InvalidVersion;
					else
					{
						//Discard the rest of the buffer.
						int remainingBytes = (int)(SIZEOF_HEADER - fs.Position);
						br.Read(new byte[remainingBytes], 0, remainingBytes);
					}
				}

				Stopwatch watch = Stopwatch.StartNew();

				fs.Read(allBytes, 0, (int)(numChunks * SIZEOF_CHUNK));

				watch.Stop();
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

		private string GetFullName(string folderName)
        {
			return SAVE_FOLDER + folderName + "/" + FILE_NAME_CHUNK + managerName + EXT_CHUNK;
		}
	}
}
