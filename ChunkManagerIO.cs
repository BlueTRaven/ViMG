using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public class ChunkManagerIO : WorldIO
    {
		public const string FILE_NAME_CHUNK = "chunks_";
		public const string EXT_CHUNK = ".vis";

		private const long SIZEOF_HEADER = sizeof(int) * 4;
		private const long SIZEOF_CHUNK = (sizeof(ushort) * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE);

		private const int VERSION = 1;
		private const int MIN_VERSION = 1;

		private readonly ChunkManager manager;
		private readonly string managerName;

		private readonly long numChunks;

		private bool loaded;
		private byte[] allBytes;

		public ChunkManagerIO(ChunkManager manager, string chunkManagerName)
        {
            this.manager = manager;
			this.managerName = chunkManagerName;

			numChunks = manager.sizeInChunks * manager.sizeInChunks * manager.sizeInChunks;
			allBytes = new byte[numChunks * SIZEOF_CHUNK];
        }

		public void Initialize()
        {

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

				//Write unused remaining header bytes
				long remainingBytes = SIZEOF_HEADER - fs.Position;
				fs.Write(new byte[remainingBytes]);

				fs.Write(allBytes);
			}
		}

		public void SerializeAll()
        {
			for (int i = 0; i < manager.sizeInChunks * manager.sizeInChunks * manager.sizeInChunks; i++)
            {
				SerializeChunk(i);
            }

			loaded = true;
        }

		//Serializes a single chunk into the local byte stream. This does not save anything to disk! If you need to save, call Save!
		public void SerializeChunk(ChunkPosition pos)
        {
			Util.ThreeDToOneD(new ValuePoint3D(pos.X, pos.Y, pos.Z), new ValuePoint3D(manager.sizeInChunks, manager.sizeInChunks, manager.sizeInChunks), out int i);

			SerializeChunk(i);
		}

		private void SerializeChunk(int i)
        {
			int chunkOffset = (int)SIZEOF_CHUNK * i;

			int offset = chunkOffset;

			Span<ushort> chunkData = manager.GetChunks()[i].GetData().GetAll();
			int len = chunkData.Length;

			for (int j = 0; j < len; j++)
			{
				allBytes[offset++] = (byte)chunkData[j];
				allBytes[offset++] = (byte)(chunkData[j] >> 8);
			}
		}

		//Loads bytes from disk. Does not fill in chunks! Use Load, then Deserialize!
		public LoadError Load(string folderName)
		{
			using (FileStream fs = new FileStream(GetFullName(folderName), FileMode.Open, FileAccess.Read, FileShare.None))
			{
				using (BinaryReader br = new BinaryReader(fs, Encoding.ASCII, true))
				{
					int currentVersion = br.ReadInt32();

					if (currentVersion < MIN_VERSION)
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

			return LoadError.Success;
		}

		//Deserializes a chunk from the local byte stream into the world. This does not load anything from the disk! If nothing has been loaded yet, this will error!
		public void DeserializeChunk(World world, ChunkPosition pos)
        {
			Util.ThreeDToOneD(new ValuePoint3D(pos.X, pos.Y, pos.Z), new ValuePoint3D(manager.sizeInChunks, manager.sizeInChunks, manager.sizeInChunks), out int i);

			DeserializeChunk(world, pos, i);
        }

        private void DeserializeChunk(World world, ChunkPosition pos, int i)
		{
			if (!loaded)
				throw new Exception("Attempted to deserialize when nothing has been loaded. Call Load first!");

			Chunk chunk = manager.GetChunk(pos);
			ChunkData data = chunk.GetData();

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
				const int NUM_BYTES_PER_CHUNK = 2;
				int offset = j * NUM_BYTES_PER_CHUNK;

				byte a = buffer[offset + 0];
				byte b = buffer[offset + 1];

				//bitwise operators are not defined for ushort, so we're forced to cast... fun.
				int id = 0;
				id |= b << 8;
				id |= a << 0;

				allCubes[j] = (ushort)id;

				//data.SetDensity(0, (ushort)id);
			}

			chunk.Initialize(world);
			data.GenStep = ChunkData.GenerationStep.Done;

			manager.MarkDirty(pos, false);
		}

		private string GetFullName(string folderName)
        {
			return SAVE_FOLDER + folderName + "/" + FILE_NAME_CHUNK + managerName + EXT_CHUNK;
		}
	}
}
