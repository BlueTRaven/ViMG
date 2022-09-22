using Microsoft.Xna.Framework.Graphics;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ViMG.Entities;

namespace ViMG
{
	public class WorldSaver
	{
		//To Save:
		//Check list of unsaved chunks. (This should be any chunk that has been modified.)
		//Save them??

		//To Save Entities:
		//Save should give a list of all chunks being saved.
		//Determine which entities live in these chunks
		//Serialize them to bytes
		//Write header
		//Write all already saved entities
		//Write newly saved entities

		private const string SAVE_FOLDER = "./saves/";
		public const string FILE_NAME_CHUNK = "world_chunks.vis";
		public const string FILE_NAME_ENTITIES = "world_entities.vis";
		public const string FILE_NAME_HEIGHTMAP = "world_heightmap.bin";
		public const string FILE_NAME_SESSION = "session.ses";

		private const long ONE_CHUNK_SIZE = (sizeof(ushort) * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE);
		private const long HEADER_OFFSET = sizeof(int) * 4;
		private const int VERSION = 1;
		private const int MIN_VERSION = 1;

		private readonly ChunkManager chunkManager;
		private readonly EntityManager entityManager;
		private readonly SessionInformation session;

		private Dictionary<ChunkPosition, List<EntityLookup>> lookups = new Dictionary<ChunkPosition, List<EntityLookup>>();
		private Dictionary<ChunkPosition, List<EntityData>> entityDatas = new Dictionary<ChunkPosition, List<EntityData>>();
		private int numLoadedEntities;

		private struct EntityLookup
		{
			public int cx, cy, cz;
			public ulong offset;
			public int size;

			public const int SIZE = 4 + 4 + 4 + 8 + 4;

			public void Serialize(List<byte> saveBytes)
			{
				SaveHelper.SaveInt32(saveBytes, cx);
				SaveHelper.SaveInt32(saveBytes, cy);
				SaveHelper.SaveInt32(saveBytes, cy);
				SaveHelper.SaveUInt64(saveBytes, offset);

				SaveHelper.SaveInt32(saveBytes, size);
			}

			public void Deserialize(byte[] loadBytes, int version)
			{
				int index = 0;
				cx = SaveHelper.LoadInt32(loadBytes, ref index);
				cy = SaveHelper.LoadInt32(loadBytes, ref index);
				cz = SaveHelper.LoadInt32(loadBytes, ref index);
				offset = SaveHelper.LoadUInt64(loadBytes, ref index);

				if (version >= 4)
					size = SaveHelper.LoadInt32(loadBytes, ref index);
			}
		}

		//The goal with entity data is to have saved data stored in memory so it can be quickly deserialized, without taking up the whole space in RAM.
		//Essentially: we don't want to load all entities at once, as they may be in chunks that are currently unloaded. 
		//Therefore we keep them in a serialized state so that when the chunk they belong to loads, it will also load the entities associated with it.
		private struct EntityData
		{
			public ulong id;
			public string type;
			public ChunkPosition position;
			public int version;
			public int size;
			public int chksum;

			public byte[] data;

            public EntityData(Entity entity)
            {
				List<byte> entityDataBlockData = new List<byte>();
				entity.OnSave(entityDataBlockData);

				position = ChunkPosition.WorldSpaceChunk(entity.Position);

				List<byte> dataBlock = new List<byte>();

				List<byte> headerBlock = new List<byte>();

				SaveHelper.SaveUInt64(headerBlock, entity.Id);
				id = entity.Id;
				SaveHelper.SaveString(headerBlock, entity.GetType().ToString());
				type = entity.GetType().ToString();

				SaveHelper.SaveInt32(headerBlock, position.X);
				SaveHelper.SaveInt32(headerBlock, position.Y);
				SaveHelper.SaveInt32(headerBlock, position.Z);

				var meta = entity.GetType().GetCustomAttribute<EntityMetaAttribute>();

				if (meta != null)
				{
					SaveHelper.SaveInt32(headerBlock, meta.Version);
					version = meta.Version;
				}
				else
				{
					Console.WriteLine("entity id " + entity.Id + " type " + entity.GetType().ToString() + " lacks a meta attribute. " +
						"This is likely not a fatal error, but all serializable entities should have a meta attribute.");
					SaveHelper.SaveInt32(headerBlock, -1);
					version = -1;
				}

				SaveHelper.SaveInt32(headerBlock, entityDataBlockData.Count);
				size = entityDataBlockData.Count;

				chksum = 0;
				for (int i = 0; i < entityDataBlockData.Count; i++)
					chksum += entityDataBlockData[i];

				SaveHelper.SaveInt32(headerBlock, chksum);

				SaveHelper.SaveBytesFlat(dataBlock, headerBlock);
				SaveHelper.SaveBytesFlat(dataBlock, entityDataBlockData);

				data = dataBlock.ToArray();
            }
        }

        public enum LoadError
		{
			Success,
			InvalidVersion
		}

		public WorldSaver(ChunkManager chunkManager, EntityManager entityManager, SessionInformation session)
		{
			this.chunkManager = chunkManager;
			this.entityManager = entityManager;
			this.session = session;
		}

		public string[] GetWorldSaveDirectories()
        {
			string[] strings;

			if (Directory.Exists(SAVE_FOLDER))
				strings = Directory.GetDirectories(SAVE_FOLDER);
			else strings = Array.Empty<string>();
			
			for (int i = 0; i < strings.Length; i++)
            {
				int ind = strings[i].LastIndexOf('/');
				strings[i] = strings[i].Substring(ind + 1);
			}

			return strings;
        }

		public bool DoesSaveExist(string folderName)
        {
			return Directory.Exists(SAVE_FOLDER + folderName + "/");
        }

		public void Save(string folderName)
		{
			Stopwatch watch = Stopwatch.StartNew();

			SaveSession();

            if (!Directory.Exists(SAVE_FOLDER + folderName))
                Directory.CreateDirectory(SAVE_FOLDER + folderName);

            var chunks = chunkManager.GetChunks();

			using (FileStream fs = new FileStream(SAVE_FOLDER + folderName + "/" + FILE_NAME_CHUNK, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, (int)ONE_CHUNK_SIZE))
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

			if (!File.Exists(SAVE_FOLDER + folderName + "/" + FILE_NAME_HEIGHTMAP))
			{
				var f = File.Create(SAVE_FOLDER + folderName + "/" + FILE_NAME_HEIGHTMAP);
				f.Close();
			}

			while (true)
			{
                try
                {
					//chunkManager.Heightmap.SetData(chunkManager.HeightmapRaw);
					float[] floats = chunkManager.HeightmapRaw;
					byte[] bytes = new byte[floats.Length * 4];
					Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);

					File.WriteAllBytes(SAVE_FOLDER + folderName + "/" + FILE_NAME_HEIGHTMAP, bytes);

                    /*using (FileStream fsHeightmap = new FileStream(SAVE_FOLDER + folderName + "/" + FILE_NAME_HEIGHTMAP, FileMode.Truncate, FileAccess.Write, FileShare.None))
					{

						//chunkManager.Heightmap.SaveAsPng(fsHeightmap, chunkManager.sizeInCubes, chunkManager.sizeInCubes);
					}*/
					break;
				}
				catch (Exception e) 
				{

				}
			}

			SaveEntities(folderName);

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

		private void SaveEntities(string folderName)
		{
			//h: header block
			//	v: version (int) overall version of the entity file
			//	emi: entity manager id (ulong) last saved entity id, to prevent entity id overlaps
			//  c: count of entities
			//	et: entity lookup table 
			//    note that the count of entities will correspond to the number of lookups here, so there's no need to save that again.
			//	  lookup table is statically sized.
			//	  cx, cy, cz: chunk x, y, z (int each) (position in chunks)
			//    o: offset into entity data block
			//	  s: size of entity in data block
			//e: entities data block
			//  e: entity data block
			//    h: header block
			//      s: size (int) includes data
			//  	i: entity id (int) (index in saved entity array)
			//	    t: type id (int)
			//	    cx, cy, cz: chunk x, y, z (int each) (position in chunks)
			//	    v: version (int)
			//	  s: size (int)
			//	  ck: chksum (int)
			//	  d: data block
			using (MemoryStream ms = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(ms, Encoding.ASCII, true))
				{
					writer.Write(4);    //version
					writer.Write(entityManager.GetUniqueId());

					var entities = entityManager.GetEntities();

					int serializableEntities = 0;

					List<Entity> entitiesToSerialize = new List<Entity>();
					List<EntityLookup> entityLookups = new List<EntityLookup>();

					foreach (Entity entity in entities)
					{
						if (entity.GetType().GetCustomAttribute<SerializableAttribute>() != null)
						{
							serializableEntities++;
							entitiesToSerialize.Add(entity);
						}
					}

					//Contains all entities' data.
					List<byte> entitiesDataBlock = new List<byte>();

					foreach (Entity entity in entitiesToSerialize)
					{
						ChunkPosition pos = ChunkPosition.WorldSpaceChunk(entity.Position);

						if (!entityDatas.ContainsKey(pos))
							entityDatas.Add(pos, new List<EntityData>());
						EntityData data = new EntityData(entity);
						entityDatas[pos].Add(data);

                        byte[] entityDataBlock = data.data;

                        entityLookups.Add(new EntityLookup()
						{
							cx = pos.X,
							cy = pos.Y,
							cz = pos.Z,
							offset = (ulong)writer.BaseStream.Position + (ulong)entitiesDataBlock.Count,
							size = entityDataBlock.Length,
						});

						SaveHelper.SaveInt32(entitiesDataBlock, entityDataBlock.Length);
						SaveHelper.SaveBytesFlat(entitiesDataBlock, entityDataBlock);
					}

					writer.Write(serializableEntities);

					List<byte> lookupBlock = new List<byte>();
					foreach (EntityLookup lookup in entityLookups)
					{
						lookup.Serialize(lookupBlock);
					}

					writer.Write(lookupBlock.Count);
					writer.Write(lookupBlock.ToArray());
					writer.Write(entitiesDataBlock.Count);
					writer.Write(entitiesDataBlock.ToArray());
				}

				using (FileStream fs = new FileStream(SAVE_FOLDER + folderName + "/" + FILE_NAME_ENTITIES, FileMode.OpenOrCreate, FileAccess.Write))
				{
					fs.Write(ms.GetBuffer());
				}
			}
		}

		private void LoadEntities(EntityManager entityManager, string folderName)
		{
			//In case of failure, keep old lookups.
			Dictionary<ChunkPosition, List<EntityLookup>> oldLookups = lookups;
			lookups = new Dictionary<ChunkPosition, List<EntityLookup>>();
			entityDatas = new Dictionary<ChunkPosition, List<EntityData>>();
			numLoadedEntities = 0;

			Console.WriteLine("Loading Entities...");

			if (!File.Exists(SAVE_FOLDER + folderName + "/" + FILE_NAME_ENTITIES))
			{
				Console.WriteLine("Could not load entities. The file world_entities.vis does not exist!");
				return;
			}

			using (FileStream fs = new FileStream(SAVE_FOLDER + folderName + "/" + FILE_NAME_ENTITIES, FileMode.Open, FileAccess.Read, FileShare.None, 1024))
			{
				using (BinaryReader reader = new BinaryReader(fs, Encoding.ASCII, false))
				{
					int version = reader.ReadInt32();
					ulong uniqueIdSeed = reader.ReadUInt64();   //unique id
					entityManager.SetUniqueIdSeed(uniqueIdSeed);

					int num = reader.ReadInt32();

					//create lookup table

					int lookupBlockSize = reader.ReadInt32();
					byte[] lookupBlock = reader.ReadBytes(lookupBlockSize);

					for (int i = 0; i < num; i++)
					{
						int begin = i * EntityLookup.SIZE;
						int end = begin + EntityLookup.SIZE;
						EntityLookup lookup = new EntityLookup();
						lookup.Deserialize(lookupBlock[begin..end], version);

						ChunkPosition cp = new ChunkPosition(lookup.cx, lookup.cy, lookup.cz);
						if (!lookups.ContainsKey(cp))
							lookups.Add(cp, new List<EntityLookup>());
						lookups[cp].Add(lookup);
					}

					int dataBlockSize = reader.ReadInt32();
					byte[] entityDataBlock = reader.ReadBytes(dataBlockSize);

					int edbI = 0;
					for (int i = 0; i < num; i++)
					{
						int headerSize = SaveHelper.LoadInt32(entityDataBlock, ref edbI);
						byte[] bytes = SaveHelper.LoadBytes(entityDataBlock, headerSize, ref edbI);

						int index = 0;
						ulong entId = SaveHelper.LoadUInt64(bytes, ref index);
						string entType = SaveHelper.LoadString(bytes, ref index);

						int cx = SaveHelper.LoadInt32(bytes, ref index);
						int cy = SaveHelper.LoadInt32(bytes, ref index);
						int cz = SaveHelper.LoadInt32(bytes, ref index);
						ChunkPosition position = new ChunkPosition(cx, cy, cz);

						int entVersion = SaveHelper.LoadInt32(bytes, ref index);
						int entDataSize = SaveHelper.LoadInt32(bytes, ref index);
						int entChksum = SaveHelper.LoadInt32(bytes, ref index);

						byte[] entData = bytes[index..];

						if (entData.Length != entDataSize)
						{
							Console.WriteLine("Could not load entity id " + entId + " type " + entType + "; read size was invalid. Is the data corrupt?");
							continue;
						}

						int chksum = 0;
						for (int d = 0; d < entDataSize; d++)
							chksum += entData[d];

						if (entChksum != chksum)
						{
							Console.WriteLine("Could not load entity id " + entId + " type " + entType + "; chksum was invalid.");
						}
						else
						{
							if (!entityDatas.ContainsKey(position))
								entityDatas.Add(position, new List<EntityData>());
							entityDatas[position].Add(new EntityData() 
							{
								id = entId,
								type = entType,
								position = position,
								size = entDataSize,
								chksum = chksum,
								version = version,

								data = entData 
							});

							numLoadedEntities++;

							//Entity ent = Activator.CreateInstance(Assembly.GetExecutingAssembly().GetName().Name, entType).Unwrap() as Entity;
							//ent.OnLoad(entData, entVersion);

							//entityManager.ForceAdd(ent, entId);
						}
					}
				}
			}
		}

		public LoadError Load(GraphicsDevice device, World world, string folderName)
		{
			Console.WriteLine("Loading Save " + folderName + "...");

			Stopwatch watch = Stopwatch.StartNew();

			if (entityManager != null)
			{
				//Load entities first.
				//This is necessary since entities aren't actually created; they stay as raw data, then are created when the chunk itself is loaded.
				LoadEntities(entityManager, folderName);
			}

			watch.Stop();

			Console.WriteLine("Loaded all " + numLoadedEntities + " entities in: " + watch.Elapsed.ToString());

			using (FileStream fs = new FileStream(SAVE_FOLDER + folderName + "/" + FILE_NAME_CHUNK, FileMode.Open, FileAccess.Read, FileShare.None, (int)ONE_CHUNK_SIZE))
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

				watch = Stopwatch.StartNew();

				//LoadByBinaryReader(fs, world, totalSize);
				LoadBySpan(fs, world, totalSize);

				watch.Stop();

				Console.WriteLine("Loaded all " + totalSize + " chunks in: " + watch.Elapsed.ToString());
			}

			if (chunkManager.HeightmapRaw == null)
			{
				if (!File.Exists(SAVE_FOLDER + folderName + "/" + FILE_NAME_HEIGHTMAP))
				{
					Console.WriteLine("Heightmap non-existant or invalid! Regenerating...");
					chunkManager.GenerateHeightmap();
				}
                else
                {
					//chunkManager.Heightmap?.Dispose();

					byte[] bytes = File.ReadAllBytes(SAVE_FOLDER + folderName + "/" + FILE_NAME_HEIGHTMAP);
					float[] floats = new float[bytes.Length / 4];
					Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);

					chunkManager.HeightmapRaw = floats;
					chunkManager.Heightmap.SetData(floats);

					/*using (FileStream fs = new FileStream(SAVE_FOLDER + folderName + "/" + FILE_NAME_HEIGHTMAP, FileMode.Open, FileAccess.Read))
					{
						chunkManager.Heightmap = Texture2D.FromStream(device, fs);
						chunkManager.HeightmapRaw = new float[chunkManager.sizeInCubes * chunkManager.sizeInCubes];
						chunkManager.Heightmap.GetData(chunkManager.HeightmapRaw);
					}*/
                }
			}

			Console.WriteLine("Loaded Save " + folderName + ".");

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

					Chunk chunk = new Chunk(chunkManager, new ChunkPosition(chunkX, chunkY, chunkZ));
					ChunkData data = chunk.GetData();
					chunkManager.SetChunk(chunk);

					ushort[] cubes = chunk.GetData().GetAll();
					for (int j = 0; j < Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE; j++)
					{
						/*int cubeX = j % Chunk.CHUNK_SIZE;
						int cubeY = (j / Chunk.CHUNK_SIZE) % Chunk.CHUNK_SIZE;
						int cubeZ = j / (Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE);*/

						ushort id = reader.ReadUInt16();

						cubes[j] = id;

						data.SetDensity(0, id);
						
						//chunk.GetData().SetCubeFast(j, reader.ReadUInt16());
					}

					chunk.Initialize(world);
					data.GenStep = ChunkData.GenerationStep.Done;
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

			Chunk chunk = chunkManager.GetChunk(new ChunkPosition(chunkX, chunkY, chunkZ));
			ChunkData data = chunk.GetData();
			/*Chunk chunk = new Chunk(chunkManager, new ChunkPosition(chunkX, chunkY, chunkZ));
			ChunkData data = chunk.GetData();
			chunkManager.SetChunk(chunk);*/
			long left = fs.Length - fs.Position;

			Span<byte> buffer = stackalloc byte[(int)Math.Min(ONE_CHUNK_SIZE, left)];
			fs.Read(buffer);

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

				data.SetDensity(0, (ushort)id);
			}

			chunk.Initialize(world);
			data.GenStep = ChunkData.GenerationStep.Done;
			//chunkManager.MarkDirty(chunkX, chunkY, chunkZ, false);

			if (entityDatas.ContainsKey(chunk.Position))
            {
				foreach (EntityData entData in entityDatas[chunk.Position])
                {
					Entity ent = Activator.CreateInstance(Assembly.GetExecutingAssembly().GetName().Name, entData.type).Unwrap() as Entity;
					ent.OnLoad(entData.data, entData.version);

					entityManager.ForceAdd(ent, entData.id);
				}

				//Remove so we don't end up saving duplicate entities.
				entityDatas.Remove(chunk.Position);
			}

			if (i % (chunkManager.sizeInChunks * chunkManager.sizeInChunks) == 0)
				Console.WriteLine("Loaded " + i + " / " + totalSize + " chunks...");
		}

		private void SaveSession()
		{
			if (!Directory.Exists(SAVE_FOLDER))
				Directory.CreateDirectory(SAVE_FOLDER);

			if (!File.Exists(SAVE_FOLDER + FILE_NAME_SESSION))
				File.Create(SAVE_FOLDER + FILE_NAME_SESSION);

			using (FileStream fs = new FileStream(SAVE_FOLDER + FILE_NAME_SESSION, FileMode.Truncate, FileAccess.Write, FileShare.None))
			{
				using (StreamWriter writer = new StreamWriter(fs))
				{
					writer.Write(session.LastLoadedSave);
				}
			}
		}

		public void LoadSession()
        {
			if (!File.Exists(SAVE_FOLDER + FILE_NAME_SESSION))
				return;

			using (FileStream fs = new FileStream(SAVE_FOLDER + FILE_NAME_SESSION, FileMode.Open, FileAccess.Read, FileShare.None))
            {
				using (StreamReader reader = new StreamReader(fs))
                {
					session.LastLoadedSave = reader.ReadLine();
                }
            }
		}
	}
}
