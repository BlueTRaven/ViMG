using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;
using ViMG.IMGUIImpl;
using static ViMG.EntityManagerIO;

namespace Engine
{
    public class PlayerManagerIO : WorldIO
    {
        private const int VERSION = 0;
        private const int MIN_VERSION = -1;
        public const string FILE_NAME_PLAYERS = "players";
        public const string EXT_PLAYERS = ".vis";

        public struct PlayerData
        {
            public EntityData entity;
            public bool isLocal;
            public int uuid;
            public int layer;
        }
        private List<PlayerData> playerDatas = new List<PlayerData>();
        private int version;

        public PlayerManagerIO()
        {
        }

        public void Save(string folderName)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

            //h: header block
            //	v: version (int) overall version of the players file
            //  c: count of players
            //p: players data block
            //  s: player data block size
            //  p: player[h-c] data blocks (EntityData blocks)
            //    e: EntityData
            //    i: is local player
            //    u: player uuid
            //    l: layer

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms, Encoding.ASCII, true))
                {
                    writer.Write(VERSION);                   //h-v: file version

                    List<byte> entitiesDataBlock = new List<byte>();

                    foreach (PlayerData data in playerDatas)
                    {
                        data.entity.Save(entitiesDataBlock);                    //p-p-e
                        SaveHelper.SaveBool(entitiesDataBlock, data.isLocal);   //p-p-i
                        SaveHelper.SaveInt32(entitiesDataBlock, data.uuid);     //p-p-u
                        SaveHelper.SaveInt32(entitiesDataBlock, data.layer);    //p-p-l
                    }

                    writer.Write(playerDatas.Count); //h-c:	count of entities

                    writer.Write(entitiesDataBlock.Count);      //h-s
                    writer.Write(entitiesDataBlock.ToArray());  //e
                }

                //return SAVE_FOLDER + folderName + "/" + FILE_NAME_ENTITIES + layer + EXT_ENTITIES;
                using (FileStream fs = new FileStream(GetPath(folderName), FileMode.OpenOrCreate, FileAccess.Write))
                {
                    fs.Write(ms.GetBuffer());
                }
            }
        }

        public LoadError Load(string folderName)
        {
            Console.WriteLine("Loading Player Datas...");

            if (!File.Exists(GetPath(folderName)))
                return LoadError.FileDoesntExist;

            using (FileStream fs = new FileStream(GetPath(folderName), FileMode.Open, FileAccess.Read, FileShare.None, 1024))
            {
                using (BinaryReader reader = new BinaryReader(fs, Encoding.ASCII, false))
                {
                    version = reader.ReadInt32();

                    if (version <= MIN_VERSION)
                        return LoadError.InvalidVersion;

                    int count = reader.ReadInt32();

                    int dataBlockSize = reader.ReadInt32();
                    byte[] entityDataBlock = reader.ReadBytes(dataBlockSize);

                    for (int i = 0; i < count; i++)
                    {
                        EntityData entity = new EntityData();
                        int bytesRead = entity.Load(entityDataBlock);
                        var isLocal = SaveHelper.LoadBool(entityDataBlock, ref bytesRead);
                        var uuid = SaveHelper.LoadInt32(entityDataBlock, ref bytesRead);
                        int layer = SaveHelper.LoadInt32(entityDataBlock, ref bytesRead);

                        playerDatas.Add(new PlayerData
                        {
                            entity = entity,
                            isLocal = isLocal,
                            uuid = uuid,
                            layer = layer,
                        });
                        entityDataBlock = entityDataBlock[bytesRead..];
                    }
                }
            }

            return LoadError.Success;
        }

        public void SerializeAll(World world)
        {
            foreach (Player player in world.player)
            {
                if (player != null && player.IsInitialized)
                {
                    Serialize(world, player.playerIndex);
                }
            }
        }

        public void Serialize(World world, int playerIndex)
        {
            Debug.Assert(world.player[playerIndex] != null);

            EntityData data = new EntityData(world.player[playerIndex]);
            playerDatas.Add(new PlayerData()
            {
                entity = data,
                layer = world.Layer,
                uuid = world.player[playerIndex].playerUuid,
                isLocal = world.player[playerIndex].IsLocalPlayer,
            });
        }

        public Player DeserializeLocal(World world)
        {
            int index = -1;
            for (int i = 0; i < playerDatas.Count; i++)
            {
                PlayerData pdata = playerDatas[i];
                if (pdata.isLocal)
                {
                    index = i;
                    break;
                }
            }

            if (index != -1)
            {
                return Deserialize(world, playerDatas[index].uuid, 0);
            }
            else
            {
                IMGUIConsole.Assert(false);
                throw new Exception();
            }
        }

        /// <summary>
        /// Returns a player object of the given player uuid. If no player exists with that uuid,
        /// a new one is created.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="playerUuid"></param>
        /// <returns>A deserialized player object with the given player uuid, if a player exists with that uuid; otherwise, a new player with the given uuid.</returns>
        public Player Deserialize(World world, int playerUuid, int playerIndex)
        {
            Player? player = null;
            PlayerData playerDataToRemove = new PlayerData
            {
                layer = -1,
            };

            foreach (PlayerData pdata in playerDatas)
            {
                if (pdata.uuid == playerUuid)
                {
                    var created = new Player(playerIndex, playerUuid);
                    created.playerUuid = playerUuid;
                    created.OnLoad(world, pdata.entity.data, pdata.entity.version);
                    try
                    {
                        world.EntityManager.ForceAdd(created, pdata.entity.id);
                        player = created;
                        playerDataToRemove = pdata;
                    }
                    catch (Exception e)
                    {
                        IMGUIConsole.Assert(false, string.Format("Deserialize: Exception encountered while deserializing player with uuid {0}. A new player will be instantiated instead\n{1}", playerUuid, e.ToString()));
                    }
                }
            }


            if (playerDataToRemove.layer != -1)
                playerDatas.Remove(playerDataToRemove);

            if (player == null) 
            {
                player = new Player(playerIndex, playerUuid, true);
                world.EntityManager.ForceAdd(player);
            }

            return player;
        }

        public void DecacheCurrentlySerialized(EntityManager manager)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

            List<PlayerData> datasToDecache = new List<PlayerData>();

            foreach (PlayerData data in playerDatas)
            {
                foreach (Player p in manager.GetAll<Player>())
                {
                    //entity is currently active; decache it
                    if (p.Id == data.entity.id)
                        datasToDecache.Add(data);
                }
            }

            foreach (PlayerData data in datasToDecache)
            {
                playerDatas.Remove(data);
            }
        }

        public override bool HandleError(LoadError error, string folderName)
        {
            throw new NotImplementedException();
        }

        private string GetPath(string folderName)
        {
            return SAVE_FOLDER + folderName + "/" + FILE_NAME_PLAYERS + EXT_PLAYERS;
        }

        public static int GetHashCodeForName(string name)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < name.Length && name[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ name[i];
                    if (i == name.Length - 1 || name[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ name[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}
