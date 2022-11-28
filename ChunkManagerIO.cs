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

		private readonly long numChunks;

		private bool loaded;
		private byte[] allBytes;

		public ChunkManagerIO(int sizeInChunks, string chunkManagerName)
        {
            //this.manager = manager;
			this.managerName = chunkManagerName;

			numChunks = sizeInChunks * sizeInChunks * sizeInChunks;
			allBytes = new byte[numChunks * SIZEOF_CHUNK];
        }

		public void Initialize()
        {

        }

		public byte[] GetBytes()
        {
			return allBytes;
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

		/*public void SerializeAll()
        {
			Chunk[] chunks = manager.GetChunks();

			for (int i = 0; i < numChunks; i++)
            {
				SerializeChunk(chunks, i);
            }

			loaded = true;
        }

		public void Serialize(IEnumerable<ChunkPosition> positions)
        {
			Chunk[] chunks = manager.GetChunks();
			foreach (ChunkPosition pos in positions)
            {
				SerializeChunk(chunks, pos);
            }
        }

		//Serializes a single chunk into the local byte stream. This does not save anything to disk! If you need to save, call Save!
		public void SerializeChunk(Chunk[] chunks, ChunkPosition pos)
        {
			Util.ThreeDToOneD(new ValuePoint3D(pos.X, pos.Y, pos.Z), new ValuePoint3D(manager.sizeInChunksXZ, manager.sizeInChunksXZ, manager.sizeInChunksXZ), out int i);

			SerializeChunk(chunks, i);
		}

		private void SerializeChunk(Chunk[] chunks, int i)
        {
			int chunkOffset = (int)SIZEOF_CHUNK * i;

			int offset = chunkOffset;

			Span<ushort> chunkData = chunks[i]?.GetData()?.GetAll();

			if (chunkData != null && !chunkData.IsEmpty)
			{
				int len = chunkData.Length;

				for (int j = 0; j < len; j++)
				{
					allBytes[offset++] = (byte)chunkData[j];
					allBytes[offset++] = (byte)(chunkData[j] >> 8);
				}
			}
            else
            {
				Console.WriteLine("Tried to serialize uninitialized chunk... fix me!");
            }
		}*/

		//Loads bytes from disk. Does not fill in chunks! Use Load, then Deserialize!
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
