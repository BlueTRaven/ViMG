using BrUtility;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;
using ViMG.Entities.Renderers;
using ViMG.IMGUIImpl;
using static ViMG.EntityManagerIO;

namespace ViMG
{
    public class EntityManagerIO : WorldIO
    {
        public const string FILE_NAME_ENTITIES = "entities";
		public const string EXT_ENTITIES = ".vis";

		/// <summary>
		/// Stores EntityData by chunk.
		/// </summary>
		public class EntityDataChunkStore
		{
			public Dictionary<ChunkPosition, List<EntityData>> entityDatas = new Dictionary<ChunkPosition, List<EntityData>>();
			public int numEntities;
			public int Version;
			public string OtherError = "";
			public ulong UniqueIdSeed;
			public ChunkPosition playerChunkPosition;
			public bool playerChunkPositionLoaded;

            public LoadError Load(string folderName, int layer)
			{
                numEntities = 0;

                Console.WriteLine("Loading Entity Datas...");

                if (!File.Exists(GetLoadName(folderName, layer)))
                    return LoadError.FileDoesntExist;

                using (FileStream fs = new FileStream(GetLoadName(folderName, layer), FileMode.Open, FileAccess.Read, FileShare.None, 1024))
                {
                    using (BinaryReader reader = new BinaryReader(fs, Encoding.ASCII, false))
                    {
                        Version = reader.ReadInt32();

                        if (Version <= MIN_VERSION)
                            return LoadError.InvalidVersion;

                        if (Version >= 6)
                        {
                            var loadedLayer = reader.ReadInt32();
                            if (loadedLayer != layer)
                            {
                                OtherError = string.Format("Tried to load a entity file as layer {0}, but it actually belongs to layer {1}!", layer, loadedLayer);
                                return LoadError.Other;
                            }
                        }

                        ulong uniqueIdSeed = reader.ReadUInt64();   //unique id
						UniqueIdSeed = uniqueIdSeed;
                        //manager.SetUniqueIdSeed(uniqueIdSeed);

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

                                numEntities++;
                            }
                        }
                    }
                }

				return LoadError.Success;
            }
		}

		//The goal with entity data is to have saved data stored in memory so it can be quickly deserialized, without taking up the whole space in RAM.
		//Essentially: we don't want to load all entities at once, as they may be in chunks that are currently unloaded. 
		//Therefore we keep them in a serialized state so that when the chunk they belong to loads, it will also load the entities associated with it.
		public struct EntityData
		{
			public ulong id;
			public string type;
			public ChunkPosition position;
			public int version;
			public int size;
			public int chksum;

			public byte[] header;
			public byte[] data;

			public bool IsValid => header != null && data != null;

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

				SaveHelper.SaveInt32(headerBlock, entityDataBlockData.Count);	//e-s
				size = entityDataBlockData.Count;

				chksum = 0;
				for (int i = 0; i < entityDataBlockData.Count; i++)
					chksum += entityDataBlockData[i];

				SaveHelper.SaveInt32(headerBlock, chksum);						//e-ck

				//SaveHelper.SaveBytesFlat(dataBlock, headerBlock);				//e-h
				SaveHelper.SaveBytesFlat(dataBlock, entityDataBlockData);       //e	TODO this is pretty much just an unnecessary copy, could just use entityDataBlockData...

				header = headerBlock.ToArray();
				data = dataBlock.ToArray();
			}

			public void Save(List<byte> bytes)
			{
				SaveHelper.SaveInt32(bytes, header.Length + data.Length);
				SaveHelper.SaveBytesFlat(bytes, header);
                SaveHelper.SaveBytesFlat(bytes, data);
            }

			public int Load(byte[] entityDataBytes)
			{
				if (entityDataBytes.Length <= 0) return 0;
				int edbI = 0;

                int headerSize = SaveHelper.LoadInt32(entityDataBytes, ref edbI);
                byte[] bytes = SaveHelper.LoadBytes(entityDataBytes, headerSize, ref edbI);

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
                    id = entId;
                    type = entType;
                    this.position = position;
                    size = entDataSize;
                    this.chksum = chksum;
                    version = entVersion;
                    header = entHeader;

					data = entBody;
                }

				return edbI;
            }
		}

		private const int VERSION = 6;
		private const int MIN_VERSION = 4;
		private readonly EntityManager manager;
        private readonly int layer;

        //Player datas are stored separately as they should immediately be deserialized on startup.
        //private ChunkPosition playerChunkPosition;
		//private bool playerChunkPositionLoaded;


		private EntityDataChunkStore datas;

		public EntityManagerIO(EntityManager entityManager, int layer)
        {
            this.manager = entityManager;
            this.layer = layer;
        }

		public void Save(string folderName)
		{
			using var zone = TracyImpl.Tracy.BeginZone();

			//h: header block
			//	v: version (int) overall version of the entity file
			//	l: layer (int) layer that this entity file belongs to
			//	emi: entity manager id (ulong) last saved entity id, to prevent entity id overlaps
			//  c: count of entities
			//e: entities data block
			//	s: header + entity data block size (total)
			//  e: entity[h-c] data blocks
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
					writer.Write(VERSION);                   //h-v: file version
					writer.Write(layer);                    //h-l: file layer
					writer.Write((ulong)0); // manager.GetUniqueId());    //h-emi: entity manager last saved entity id

					int serializableEntities = 0;

					List<byte> entitiesDataBlock = new List<byte>();

					foreach (List<EntityData> datas in datas.entityDatas.Values)
					{
						foreach (EntityData data in datas)
						{
							data.Save(entitiesDataBlock);
							//SaveHelper.SaveInt32(entitiesDataBlock, data.header.Length + data.data.Length);  //e-s
							//SaveHelper.SaveBytesFlat(entitiesDataBlock, data.header);	//e-h
							//SaveHelper.SaveBytesFlat(entitiesDataBlock, data.data);		//e-e

							serializableEntities++;
						}
					}

					writer.Write(serializableEntities); //h-c:	count of entities

					writer.Write(entitiesDataBlock.Count);      //h-s: size of entity block
					writer.Write(entitiesDataBlock.ToArray());  //e
				}

                using (FileStream fs = new FileStream(GetSaveName(folderName, layer), FileMode.OpenOrCreate, FileAccess.Write))
				{
					fs.Write(ms.GetBuffer());
				}
			}
		}

		public void SerializeAll(int sizeInChunks)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

			if (datas == null) datas = new EntityDataChunkStore();

            for (int z = 0; z < sizeInChunks; z++)
            {
				for (int y = 0; y < sizeInChunks; y++)
                {
					for (int x = 0; x < sizeInChunks; x++)
                    {
						Serialize(new ChunkPosition(x, y, z));
                    }
				}
			}
        }

		public void Serialize(IEnumerable<ChunkPosition> positions)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            foreach (ChunkPosition pos in positions)
            {
				Serialize(pos);
            }
        }

		public void Serialize(ChunkPosition pos)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

            //TODO: entitiesByChunk or something similar so we don't have to loop through all entities to determine whether or not they should be serialized.

            var entities = manager.GetEntities();

			List<Entity> entitiesToSerialize = new List<Entity>();

			foreach (Entity entity in entities)
			{
				if (entity is Player) continue;

				ChunkPosition entityPos = ChunkPosition.WorldSpaceChunk(entity.Position);

				if (entityPos == pos)
				{
					var serializableAttribute = entity.GetType().GetCustomAttribute<EntitySerializableAttribute>();
					if ((serializableAttribute != null && 
						(serializableAttribute.serializationType & EntitySerializableAttribute.SerializationType.World) == EntitySerializableAttribute.SerializationType.World) ||
						entity.Serialize)	//entities can force a serialize if Serialize is true. Typically used for structure-spawned entities.
					{
						entitiesToSerialize.Add(entity);
					}
				}
			}

			//Console.WriteLine("Saving {0} entities...", entitiesToSerialize.Count);

			foreach (Entity entity in entitiesToSerialize)
			{
				//if (entity is Player)
				//{
				//	playerChunkPosition = ChunkPosition.WorldSpaceChunk(entity.Position);
				//	playerChunkPositionLoaded = true;
				//}
				if (!datas.entityDatas.ContainsKey(pos))
					datas.entityDatas.Add(pos, new List<EntityData>());
				EntityData data = new EntityData(entity);
				datas.entityDatas[pos].Add(data);
			}

			//Console.WriteLine("Done.");
		}

		//Sometimes we need to save and keep the world loaded. In this case, cached entities will be duplicates of
		//entities that already exist. We typically unload cached entity datas when we deserialize them to prevent this,
		//but since we still have to keep the world loaded in this scenario, we have to manually check and decache active entities.
		//TODO: optimize!
		public void DecacheCurrentlySerialized()
		{
            using var zone = TracyImpl.Tracy.BeginZone();

            foreach (List<EntityData> datas in datas.entityDatas.Values)
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
            using var zone = TracyImpl.Tracy.BeginZone();

			datas = new EntityDataChunkStore();
			var error = datas.Load(folderName, layer);
			return error;
		}

		public void Deserialize(ChunkPosition pos)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

			if (datas != null && datas.entityDatas.ContainsKey(pos))
            {
				foreach (EntityData entData in datas.entityDatas[pos])
                {
					DeserializeEntity(entData);
				}

				//Remove so we don't end up saving duplicate entities.
				datas.entityDatas.Remove(pos);
			}
		}

		public Entity? DeserializeEntity(EntityData entData, int overrideGeneration = -1)
		{
            Type entityType = Utility.GetType(entData.type);

            if (entityType == null)
            {
                Console.WriteLine("Could not deserialize an entity with type name {0}. Has the name changed in code?\nThis is not fatal! Entity will not load.", entData.type);
            }
            else
            {
				if (entityType.GetConstructor([]) != null)
				{
					var created = Activator.CreateInstance(entityType);

					if (created != null && created is Entity ent)
					{
						ent.OnLoad(entData.data, entData.version);
						try
						{
							manager.ForceAdd(ent, entData.id, overrideGeneration);
							return ent;
						}
						catch (Exception e)
						{
                            IMGUIConsole.Assert(false, string.Format("DeserializeEntity: Exception encountered while deserializing entity with type {0}\n{1}", entData.type, e.ToString()));
						}
					}
					else
					{
						Console.WriteLine("Deserialized an entity with type name {0}, but could not cast it. Does the type extend Entity?\nThis is not fatal! Entity will not load.", entData.type);
					}
				} else
				{
					Console.WriteLine("Could not deserialize an entity with type name {0}. Forgot to add a parameterless constructor.", entData.type);
				}
            }

			return null;
        }

		private static string GetSaveName(string folderName, int layer)
        {
			return SAVE_FOLDER + folderName + "/" + FILE_NAME_ENTITIES + layer + EXT_ENTITIES;
		}

		private static string GetLoadName(string folderName, int layer)
        {
			if (!File.Exists(SAVE_FOLDER + folderName + "/" + FILE_NAME_ENTITIES + layer + EXT_ENTITIES))
				return SAVE_FOLDER + folderName + "/" + FILE_NAME_ENTITIES + EXT_ENTITIES;
			else return SAVE_FOLDER + folderName + "/" + FILE_NAME_ENTITIES + layer + EXT_ENTITIES;
		}

		public override bool HandleError(LoadError error, string folderName)
		{
			switch (error)
			{
				case LoadError.InvalidVersion:
					Console.WriteLine("Entity file could not be loaded. The current file version ({0}) is not supported.", datas.Version);
					return true;
				case LoadError.FileDoesntExist:
					Console.WriteLine("Entity file does not exist.", GetLoadName(folderName, layer));
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

		public void TestConsistency(Entity entity)
		{
			List<byte> bytes = new();
			var entData = new EntityData(entity);
			entData.Save(bytes);

			var loadedEntData = new EntityData();
			loadedEntData.Load(bytes.ToArray());

            IMGUIConsole.Assert(loadedEntData.IsValid);
		}
	}
}
