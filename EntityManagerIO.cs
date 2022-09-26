using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;

namespace ViMG
{
    public class EntityManagerIO : WorldIO
    {
        public const string FILE_NAME_ENTITIES = "world_entities.vis";

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

			public void Deserialize(byte[] loadBytes, int fileVersion)
			{
				int index = 0;
				cx = SaveHelper.LoadInt32(loadBytes, ref index);
				cy = SaveHelper.LoadInt32(loadBytes, ref index);
				cz = SaveHelper.LoadInt32(loadBytes, ref index);
				offset = SaveHelper.LoadUInt64(loadBytes, ref index);

				if (fileVersion >= 4)
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

			public byte[] header;
			public byte[] data;

			public EntityData(Entity entity)
			{
				//e: entity data block
				//  h: header block
				//    s: size (int) includes data
				//	  i: entity id (int) (index in saved entity array)
				//    t: type id (string)
				//    cx, cy, cz: chunk x, y, z (int each) (position in chunks)
				//    v: version (int)
				//  s: size (int)
				//  ck: chksum (int)
				//  d: data block
				List<byte> entityDataBlockData = new List<byte>();
				entity.OnSave(entityDataBlockData);

				position = ChunkPosition.WorldSpaceChunk(entity.Position);

				List<byte> dataBlock = new List<byte>();

				List<byte> headerBlock = new List<byte>();

				SaveHelper.SaveUInt64(headerBlock, entity.Id);						//h-i
				id = entity.Id;
				SaveHelper.SaveString(headerBlock, entity.GetType().ToString());	//h-t
				type = entity.GetType().ToString();

				SaveHelper.SaveInt32(headerBlock, position.X);						//h-cxyz
				SaveHelper.SaveInt32(headerBlock, position.Y);
				SaveHelper.SaveInt32(headerBlock, position.Z);

				var meta = entity.GetType().GetCustomAttribute<EntityMetaAttribute>();

				if (meta != null)
				{
					SaveHelper.SaveInt32(headerBlock, meta.Version);	//h-v
					version = meta.Version;
				}
				else
				{	
					Console.WriteLine("entity id " + entity.Id + " type " + entity.GetType().ToString() + " lacks a meta attribute. " +
						"This is likely not a fatal error, but all serializable entities should have a meta attribute.");
					SaveHelper.SaveInt32(headerBlock, -1);				//h-v
					version = -1;
				}

				SaveHelper.SaveInt32(headerBlock, entityDataBlockData.Count);	//s
				size = entityDataBlockData.Count;

				chksum = 0;
				for (int i = 0; i < entityDataBlockData.Count; i++)
					chksum += entityDataBlockData[i];

				SaveHelper.SaveInt32(headerBlock, chksum);						//ck

				//SaveHelper.SaveBytesFlat(dataBlock, headerBlock);				//e-h
				SaveHelper.SaveBytesFlat(dataBlock, entityDataBlockData);       //e

				header = headerBlock.ToArray();
				data = dataBlock.ToArray();
			}
		}

		private readonly EntityManager manager;

		//Player datas are stored separately as they should immediately be deserialized on startup.
		private ChunkPosition playerChunkPosition;
		private bool playerChunkPositionLoaded;
		private Dictionary<ChunkPosition, List<EntityLookup>> lookups = new Dictionary<ChunkPosition, List<EntityLookup>>();
        private Dictionary<ChunkPosition, List<EntityData>> entityDatas = new Dictionary<ChunkPosition, List<EntityData>>();
        private int numLoadedEntities;

		private bool loaded;

		public int Version;

		public EntityManagerIO(EntityManager entityManager)
        {
            this.manager = entityManager;
        }

		public void Save(string folderName)
        {
			//h: header block
			//	v: version (int) overall version of the entity file
			//	emi: entity manager id (ulong) last saved entity id, to prevent entity id overlaps
			//  c: count of entities
			//e: entities data block
			//	s: header + entity data block size (total)
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
					writer.Write(5);						//h-v: file version
					writer.Write(manager.GetUniqueId());	//h-emi: entity manager last saved entity id

					var entities = manager.GetEntities();

					int serializableEntities = 0;

					List<byte> entitiesDataBlock = new List<byte>();

					foreach (List<EntityData> datas in entityDatas.Values)
                    {
						foreach (EntityData data in datas)
                        {
							SaveHelper.SaveInt32(entitiesDataBlock, data.header.Length + data.data.Length);  //e-s
							SaveHelper.SaveBytesFlat(entitiesDataBlock, data.header);	//e-h
							SaveHelper.SaveBytesFlat(entitiesDataBlock, data.data);		//e-e

							serializableEntities++;
						}
					}

					writer.Write(serializableEntities);	//h-c:	count of entities

					writer.Write(entitiesDataBlock.Count);		//h-s: size of entity block
					writer.Write(entitiesDataBlock.ToArray());	//e
				}

				using (FileStream fs = new FileStream(SAVE_FOLDER + folderName + "/" + FILE_NAME_ENTITIES, FileMode.OpenOrCreate, FileAccess.Write))
				{
					fs.Write(ms.GetBuffer());
				}
			}
		}

		public void SerializeAll(World world)
        {
			for (int x = 0; x < world.sizeInChunks; x++)
            {
				for (int y = 0; y < world.sizeInChunks; y++)
                {
					for (int z = 0; z < world.sizeInChunks; z++)
                    {
						Serialize(new ChunkPosition(x, y, z));
                    }
				}
			}

			loaded = true;
        }

		public void Serialize(IEnumerable<ChunkPosition> positions)
        {
			foreach (ChunkPosition pos in positions)
            {
				Serialize(pos);
            }
        }

		public void Serialize(ChunkPosition pos)
		{
			//TODO: entitiesByChunk or something similar so we don't have to loop through all entities to determine whether or not they should be serialized.

			var entities = manager.GetEntities();

			List<Entity> entitiesToSerialize = new List<Entity>();

			foreach (Entity entity in entities)
			{
				if (entity.GetType().GetCustomAttribute<SerializableAttribute>() != null)
				{
					ChunkPosition entityPos = ChunkPosition.WorldSpaceChunk(entity.Position);

					if (entityPos == pos)
						entitiesToSerialize.Add(entity);
				}
			}

			foreach (Entity entity in entitiesToSerialize)
			{
				if (entity is Player)
				{
					playerChunkPosition = ChunkPosition.WorldSpaceChunk(entity.Position);
					playerChunkPositionLoaded = true;
				}
				if (!entityDatas.ContainsKey(pos))
					entityDatas.Add(pos, new List<EntityData>());
				EntityData data = new EntityData(entity);
				entityDatas[pos].Add(data);
			}
		}

		//Sometimes we need to save and keep the world loaded. In this case, cached entities will be duplicates of
		//entities that already exist. We typically unload cached entity datas when we deserialize them to prevent this,
		//but since we still have to keep the world loaded in this scenario, we have to manually check and decache active entities.
		public void DecacheCurrentlySerialized()
		{
			foreach (List<EntityData> datas in entityDatas.Values)
			{
				List<EntityData> datasToDecache = new List<EntityData>();

				foreach (EntityData data in datas)
				{
					foreach (Entity e in manager.GetEntities())
                    {
						//entity is currently active; decache it
						if (e.Id == data.id)
							datasToDecache.Add(data);
                    }
				}

				foreach (EntityData data in datasToDecache)
                {
					datas.Remove(data);
                }
			}
		}

        public LoadError Load(string folderName)
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
				return LoadError.FileDoesntExist;
			}

			using (FileStream fs = new FileStream(SAVE_FOLDER + folderName + "/" + FILE_NAME_ENTITIES, FileMode.Open, FileAccess.Read, FileShare.None, 1024))
			{
				using (BinaryReader reader = new BinaryReader(fs, Encoding.ASCII, false))
				{
					Version = reader.ReadInt32();

					if (Version <= 4)
						return LoadError.InvalidVersion;

					ulong uniqueIdSeed = reader.ReadUInt64();   //unique id
					manager.SetUniqueIdSeed(uniqueIdSeed);

					int num = reader.ReadInt32();

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

						byte[] entHeader = bytes[..index];
						byte[] entBody = bytes[index..];

						if (entBody.Length != entDataSize)
						{
							Console.WriteLine("Could not load entity id " + entId + " type " + entType + "; read size was invalid. Is the data corrupt?");
							continue;
						}

						int chksum = 0;
						for (int d = 0; d < entDataSize; d++)
							chksum += entBody[d];

						if (entChksum != chksum)
						{
							Console.WriteLine("Could not load entity id " + entId + " type " + entType + "; chksum was invalid.");
						}
						else
						{
							if (entType == typeof(Player).ToString())
							{
								playerChunkPosition = position;
								playerChunkPositionLoaded = true;
							}
							if (!entityDatas.ContainsKey(position))
								entityDatas.Add(position, new List<EntityData>());
							entityDatas[position].Add(new EntityData()
							{
								id = entId,
								type = entType,
								position = position,
								size = entDataSize,
								chksum = chksum,
								version = entVersion,

								header = entHeader,
								data = entBody
							});

							numLoadedEntities++;
						}
					}
				}
			}

			loaded = true;
			return LoadError.Success;
		}

		public void DeserializePlayerChunk()
        {
			if (playerChunkPositionLoaded)
				Deserialize(playerChunkPosition);
        }

		public void Deserialize(ChunkPosition pos)
		{
			if (!loaded)
				throw new Exception("Attempted to deserialize when nothing has been loaded. Call Load first!");

			if (entityDatas.ContainsKey(pos))
            {
				foreach (EntityData entData in entityDatas[pos])
                {
					Entity ent = Activator.CreateInstance(Assembly.GetExecutingAssembly().GetName().Name, entData.type).Unwrap() as Entity;
					ent.OnLoad(entData.data, entData.version);

					manager.ForceAdd(ent, entData.id);
				}

				//Remove so we don't end up saving duplicate entities.
				entityDatas.Remove(pos);
			}
		}
    }
}
