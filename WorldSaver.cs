using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using ViMG.Entities;

namespace ViMG
{
	public class WorldSaver
	{
		public const string FILE_NAME_CHUNK = "world_chunks.vis";
		public const string FILE_NAME_ENTITIES = "world_entities.vis";

		private const long ONE_CHUNK_SIZE = (sizeof(ushort) * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE);
		private const long HEADER_OFFSET = sizeof(int) * 4;
		private const int VERSION = 1;
		private const int MIN_VERSION = 1;

		private readonly ChunkManager chunkManager;
		private readonly EntityManager entityManager;

		public enum LoadError
		{
			Success,
			InvalidVersion
		}

		public WorldSaver(ChunkManager chunkManager, EntityManager entityManager)
		{
			this.chunkManager = chunkManager;
			this.entityManager = entityManager;
		}

		public void Save()
		{
			Stopwatch watch = Stopwatch.StartNew();

			var chunks = chunkManager.GetChunks();

			using (FileStream fs = new FileStream("./" + FILE_NAME_CHUNK, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, (int)ONE_CHUNK_SIZE * chunks.Length))
			{
				fs.Write(BitConverter.GetBytes(VERSION));
				
				//Write unused remaining header bytes
				long remainingBytes = HEADER_OFFSET - fs.Position;
				fs.Write(new byte[remainingBytes]);

				for (int i = 0; i < chunks.Length; i++)
				{
					SaveOneSpan(fs, chunks, i);
				}
			}

			SaveEntities();

			watch.Stop();

			Console.WriteLine("Saved to " + FILE_NAME_CHUNK + " in " + watch.Elapsed.ToString());
		}

		private void SaveOneSpan(Stream stream, Chunk[] chunks, int chunkIndex)
		{
			Span<byte> savedChunks = stackalloc byte[(int)ONE_CHUNK_SIZE];
			int offset = 0;

			Span<ushort> chunkData = chunks[chunkIndex].GetData().GetAll();
			int len = chunkData.Length;

			for (int j = 0; j < len; j++)
			{
				savedChunks[offset++] = (byte)chunkData[j];
				savedChunks[offset++] = (byte)(chunkData[j] >> 8);
			}

			stream.Write(savedChunks);
		}

		[Obsolete("no longer supporting this method", true)]
		public void SaveOne(ChunkPosition position)
		{
			Chunk chunk = chunkManager.GetChunk(position);

			if (chunk.GetData().GenStep == ChunkData.GenerationStep.Broad)
				throw new Exception("Cannot save sentinel chunk");

			using (MemoryStream stream = new MemoryStream((int)ONE_CHUNK_SIZE))
			{
				using (BinaryWriter bwr = new BinaryWriter(stream, Encoding.ASCII, true))
				{
					for (int j = 0; j < Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE; j++)
					{
						bwr.Write(chunk.GetData().GetRaw(j));
					}
				}

				using (FileStream fs = new FileStream("./" + FILE_NAME_CHUNK, FileMode.OpenOrCreate, FileAccess.Write))
				{
					long chunkOff = ONE_CHUNK_SIZE * (position.X + chunkManager.sizeInChunks * (position.Y + chunkManager.sizeInChunks * position.Z));
					fs.Seek(chunkOff, SeekOrigin.Begin);

					fs.Write(stream.GetBuffer());
				}
			}


			//Can we just get away with no formatting? just a version number + a hunk of entities

			//To read:
			//We read the entire entity hunk regardless of whether or not we're loading a single entity in or not. 
			//read version
			//read entity manager id, set entity manager id (SetUniqueIdSeed)
			//read entity
			//read id
			//	check id with world. If the world already contains an *active* entity with this id, the seek to the end of the entity definition, ignore the rest.
			//read type id, decode id, create entity of id type
			//read cx, cy, cz into a position for later
			//read version
			//read the rest until we have read (entity start offset - size) bytes
			//	pass as List<byte> to the entity we created
			//

			//Save all entities regardless of whether we're saving one chunk or not
			//SaveEntities();
		}

		private void SaveEntities()
		{
			//v: version (int) overall version of the entity file
			//emi: entity manager id (ulong) last saved entity id, to prevent entity id overlaps
			//c: count of entities
			//e: entity
			//	i: entity id (int) (index in saved entity array)
			//	t: type id (int)
			//	cx, cy, cz: chunk x, y, z (int each) (position in chunks)
			//	v: version (int)
			//	s: size (int)
			//	ck: chksum (int)
			//	d: data (variable)
			using (MemoryStream ms = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(ms, Encoding.ASCII, true))
				{
					writer.Write(1);    //version
					writer.Write(entityManager.GetUniqueId());

					var entities = entityManager.GetEntities();

					int serializableEntities = 0;

					foreach (Entity entity in entities)
					{
						if (entity.GetType().GetCustomAttribute<SerializableAttribute>() != null)
							serializableEntities++;
					}

					writer.Write(serializableEntities);

					foreach (Entity entity in entities)
					{
						if (entity.GetType().GetCustomAttribute<SerializableAttribute>() != null)
						{
							writer.Write(entity.Id);
							writer.Write(entity.GetType().ToString());

							ChunkPosition pos = ChunkPosition.WorldSpaceChunk(entity.Position);
							writer.Write(pos.X);
							writer.Write(pos.Y);
							writer.Write(pos.Z);

							var meta = entity.GetType().GetCustomAttribute<EntityMetaAttribute>();

							if (meta != null)
								writer.Write(meta.Version);

							List<byte> data = new List<byte>();
							entity.OnSave(data);

							writer.Write(data.Count);

							int chksum = 0;
							for (int i = 0; i < data.Count; i++)
								chksum += data[i];

							writer.Write(chksum);

							writer.Write(data.ToArray());
						}
					}
				}

				using (FileStream fs = new FileStream("./" + FILE_NAME_ENTITIES, FileMode.OpenOrCreate, FileAccess.Write))
				{
					fs.Write(ms.GetBuffer());
				}
			}
		}

		private void LoadEntities(EntityManager entityManager)
		{
			Console.WriteLine("Loading Entities...");

			if (!File.Exists("./" + FILE_NAME_ENTITIES))
			{
				Console.WriteLine("Could not load entities. The file world_entities.vis does not exist!");
				return;
			}

			using (FileStream fs = new FileStream("./" + FILE_NAME_ENTITIES, FileMode.Open, FileAccess.Read, FileShare.None, 1024))
			{
				using (BinaryReader reader = new BinaryReader(fs, Encoding.ASCII, false))
				{
					int version = reader.ReadInt32();
					ulong uniqueIdSeed = reader.ReadUInt64();   //unique id
					entityManager.SetUniqueIdSeed(uniqueIdSeed);

					int num = reader.ReadInt32();

					for (int i = 0; i < num; i++)
					{
						ulong entId = reader.ReadUInt64();
						string entType = reader.ReadString();

						int cx = reader.ReadInt32();
						int cy = reader.ReadInt32();
						int cz = reader.ReadInt32();

						int entVersion = reader.ReadInt32();
						int entDataSize = reader.ReadInt32();
						int entChksum = reader.ReadInt32();

						byte[] entData = reader.ReadBytes(entDataSize);

						int chksum = 0;
						for (int d = 0; d < entDataSize; d++)
							chksum += entData[d];

						if (entChksum != chksum)
						{
							Console.WriteLine("Could not load entity id " + entId + " type " + entType + "; chksum was invalid.");
						}
						else
						{
							Entity ent = Activator.CreateInstance(Assembly.GetExecutingAssembly().GetName().Name, entType).Unwrap() as Entity;
							ent.OnLoad(entData, entVersion);

							entityManager.ForceAdd(ent, entId);
						}
					}
				}
			}
		}

		public LoadError Load(World world)
		{
			Console.WriteLine("Loading Save");

			using (FileStream fs = new FileStream("./" + FILE_NAME_CHUNK, FileMode.Open, FileAccess.Read, FileShare.None, (int)ONE_CHUNK_SIZE))
			{
				using (BinaryReader br = new BinaryReader(fs, Encoding.ASCII, true))
				{
					int currentVersion = br.ReadInt32();

					if (currentVersion < MIN_VERSION)
						return LoadError.InvalidVersion;
					else
					{
						int remainingBytes = (int)(HEADER_OFFSET - fs.Position);
						br.Read(new byte[remainingBytes], 0, remainingBytes);
					}
				}

				int totalSize = chunkManager.sizeInChunks * chunkManager.sizeInChunks * chunkManager.sizeInChunks;

				Stopwatch watch = Stopwatch.StartNew();

				//LoadByBinaryReader(fs, world, totalSize);
				LoadBySpan(fs, world, totalSize);

				watch.Stop();

				Console.WriteLine("Loaded all " + totalSize + " chunks in: " + watch.Elapsed.ToString());
			}

			LoadEntities(entityManager);

			return LoadError.Success;
		}

		private Queue<ChunkPosition> loadOnePositions = new Queue<ChunkPosition>();

		/*public void LoadOne(ChunkPosition position)
		{
			loadOnePositions.Enqueue(position);
		}

		public void ProcessLoadQueue(World world)
		{
			using (FileStream fs = new FileStream("./world.vis", FileMode.Open, FileAccess.Read, FileShare.None, (int)ONE_CHUNK_SIZE))
			{
				while (loadOnePositions.Count > 0)
				{
					ChunkPosition position = loadOnePositions.Dequeue();

					long chunkOff = ONE_CHUNK_SIZE * (position.X + chunkManager.sizeInChunks * (position.Y + chunkManager.sizeInChunks * position.Z));
					fs.Seek(chunkOff, SeekOrigin.Begin);

					LoadChunk(fs, position, world);
				}
			}
		}*/

		private void LoadBySpan(FileStream fs, World world, int totalSize)
		{
			for (int i = 0; i < totalSize; i++)
			{
				LoadChunk(fs, i, world, totalSize);
			}
		}

		private void LoadByBinaryReader(FileStream fs, World world, int totalSize)
		{
			fs.Seek(0, SeekOrigin.Begin);

			using (BinaryReader reader = new BinaryReader(fs, Encoding.ASCII))
			{
				for (int i = 0; i < totalSize; i++)
				{
					int chunkX = i % chunkManager.sizeInChunks;
					int chunkY = (i / chunkManager.sizeInChunks) % chunkManager.sizeInChunks;
					int chunkZ = i / (chunkManager.sizeInChunks * chunkManager.sizeInChunks);

					Chunk chunk = new Chunk(chunkManager.ChunkDatas, new ChunkPosition(chunkX, chunkY, chunkZ));
					chunkManager.SetChunk(chunk);

					for (int j = 0; j < Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE; j++)
					{
						int cubeX = j % Chunk.CHUNK_SIZE;
						int cubeY = (j / Chunk.CHUNK_SIZE) % Chunk.CHUNK_SIZE;
						int cubeZ = j / (Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE);

						chunk.GetData().SetCubeFast(j, reader.ReadUInt16());
					}

					chunk.Initialize(world);
					chunk.GetData().GenStep = ChunkData.GenerationStep.Done;
					chunkManager.MarkDirty(chunkX, chunkY, chunkZ, false);

					if (i % (chunkManager.sizeInChunks * chunkManager.sizeInChunks) == 0)
						Console.WriteLine("Loaded " + i + " / " + totalSize + " chunks...");
				}
			}
		}

		private void LoadChunk(FileStream fs, int i, World world, int totalSize)
		{
			int chunkX = i % chunkManager.sizeInChunks;
			int chunkY = (i / chunkManager.sizeInChunks) % chunkManager.sizeInChunks;
			int chunkZ = i / (chunkManager.sizeInChunks * chunkManager.sizeInChunks);

			Chunk chunk = new Chunk(chunkManager.ChunkDatas, new ChunkPosition(chunkX, chunkY, chunkZ));
			chunkManager.SetChunk(chunk);
			long left = fs.Length - fs.Position;

			Span<byte> buffer = stackalloc byte[(int)Math.Min(ONE_CHUNK_SIZE, left)];
			fs.Read(buffer);

			for (int j = 0; j < Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE; j++)
			{
				const int NUM_BYTES_PER_CHUNK = 2;
				int offset = j * NUM_BYTES_PER_CHUNK;

				byte a = buffer[offset + 0];
				byte b = buffer[offset + 1];

				int id = 0;
				id |= b << 8;
				id |= a << 0;

				chunk.GetData().SetCubeFast(j, (ushort)id);
			}

			chunk.Initialize(world);
			chunk.GetData().GenStep = ChunkData.GenerationStep.Done;
			//chunkManager.MarkDirty(chunkX, chunkY, chunkZ, false);

			if (i % (chunkManager.sizeInChunks * chunkManager.sizeInChunks) == 0)
				Console.WriteLine("Loaded " + i + " / " + totalSize + " chunks...");
		}
	}
}
