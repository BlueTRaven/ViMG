using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.WorldLogics;

namespace ViMG
{
    public class WorldInfoIO : WorldIO
    {
        public struct WorldInfo : INetSerializable
        {
            public int version;

            public float time;
            public Vector3[] playerPositions;
            public int[] playerLayers;
            public Vector3 spawnPosition;
            public int spawnLayer;
            public int furthestLayer;   //the furthest the player has traveled - i.e. layer+1 has NOT been generated yet.
            public List<PointOfInterest> pointsOfInterest;

            public WorldFlags flags;

            public List<Housing> housings;

            public static WorldInfo Empty => new WorldInfo()
            {
                version = 0,
                time = 0,
                playerPositions = { },
                playerLayers = { },
                spawnPosition = Vector3.Zero,
                spawnLayer = 0,
                furthestLayer = 0,
                pointsOfInterest = { },
                flags = new WorldFlags(),
                housings = { },
            };

            public void Deserialize(NetDataReader reader)
            {
                int len = reader.GetInt();
                byte[] bytes = new byte[len];
                reader.GetBytes(bytes, len);
                MemoryStream stream = new MemoryStream(bytes);
                new WorldInfoIO().LoadFromStream(stream, out this);
            }

            public void Serialize(NetDataWriter writer)
            {
                MemoryStream stream = new MemoryStream();
                new WorldInfoIO().SaveToStream(this, stream);
                writer.Put(stream.Length);
                writer.PutBytesWithLength(stream.GetBuffer());
            }
        }
        /*private struct WorldInfo
        {
            public struct Header
            {
                public int version;
            }
            
            public struct POIsBlock
            {
                public struct Header
                {
                    public int size;
                    public int num;
                }
                public struct POIBlock
                {
                    public struct Header
                    {
                        public int size;
                        public int version;
                    }
                    public Header header;
                    public PointOfInterest poi;
                }

                public Header header;
                public POIBlock[] poiBlocks;
            }

            public Header header;
            public float worldTime;
            public POIsBlock poisBlock;

            //TODO: finish
            public void Save(List<byte> bytes)
            {
                SaveHelper.SaveInt32(bytes, header.version);
                SaveHelper.SaveFloat32(bytes, worldTime);

                List<byte> poisBytes = new List<byte>();

                for (int i = 0; i < poisBlock.poiBlocks.Length; i++)
                {
                    List<byte> poiBytes = new List<byte>();
                    poisBlock.poiBlocks[i].poi.OnSave(poiBytes);
                    poisBlock.poiBlocks[i].header.size = poiBytes.Count;

                    SaveHelper.SaveInt32(poisBytes, poisBlock.poiBlocks[i].header.version);
                    SaveHelper.SaveInt32(poisBytes, poisBlock.poiBlocks[i].header.size);
                    SaveHelper.SaveBytesFlat(poisBytes, poiBytes);
                }
                
                SaveHelper.SaveBytesFlat(bytes, poisBytes);
            }
        }*/

        public const string FILE_NAME_WINFO_OLD = "world";
        public const string FILE_NAME_WINFO = "winfo";
        public const string EXT_WINFO = ".vis";

        private const int VERSION = 5;
        private const int MIN_VERSION = 0;

        public int Version;

        public WorldInfoIO()
        {
        }

        public void Save(string folderName, WorldInfo info)
        {
            /*WorldInfo winfo = new WorldInfo()
            {
                header = new WorldInfo.Header()
                {
                    version = VERSION
                },
                worldTime = world.GetTime(),
                poiBlocksHeader = new WorldInfo.POIBlocksHeader()
                {
                    size = -1,
                    num = world.PointsOfInterest.Count
                },
                poiBlocks = new WorldInfo.POIBlock[world.PointsOfInterest.Count]
            };

            for (int i = 0; i < world.PointsOfInterest.Count; i++)
            {
                PointOfInterest poi = world.PointsOfInterest[i];

                winfo.poiBlocks[i] = new WorldInfo.POIBlock()
                {
                    header = new WorldInfo.POIBlock.Header()
                    {
                        version = PointOfInterest.VERSION,
                        size = -1,
                    },
                    poi = poi,
                };
            }*/

            //h: header block
            //  v: version (int) version of worldinfo file
            //wi: worldinfo block
            //  t: world time (float)
            //  fl: furthest layer (int)
            //  px, py, pz: player xyz (Vector3)
            //  pl: player layer (int)
            //  f: flags block
            //      fh: header block
            //          v: version (int)
            //          s: size (int) of data block
            //      f: flags (int)
            //  pois: points of interest array block
            //      h: header block
            //          s: size (int) of data block
            //          n: number of elements
            //      poi: poi data blocks
            //          h:
            //              s: size (int) of data block
            //              v: version (int) of poi
            //          d: poi data block
            //              cx, cy, cz: cube x,y,z (CubePosition) of poi
            //              n: name (string) of point of interest
            //              w: weight (int)
            //  hs: housing block
            //      h:
            //          s: size (int) of data block
            //          n: number of elements
            //      h: housing data blocks
            //          See HousingTasker.cs for serialization info

            if (!Directory.Exists(SAVE_FOLDER + folderName))
                Directory.CreateDirectory(SAVE_FOLDER + folderName);

            //FileStream is probably unnecessary since we're saving the everything all at once
            using (FileStream fs = new FileStream(GetFullName(folderName), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                SaveToStream(info, fs);
            }
        }

        public void SaveToStream(WorldInfo info, Stream stream)
        {
            List<byte> bytes = new List<byte>();

            SaveHelper.SaveInt32(bytes, VERSION); //h-v

            SaveHelper.SaveFloat32(bytes, info.time); //wi-t

            SaveHelper.SaveInt32(bytes, info.furthestLayer);

            for (int i = 0; i < info.playerPositions.Length; i++)
            {
                SaveHelper.SaveVector3(bytes, info.playerPositions[i]);
                SaveHelper.SaveInt32(bytes, info.playerLayers[i]);
            }
            SaveHelper.SaveVector3(bytes, info.spawnPosition);
            SaveHelper.SaveInt32(bytes, info.spawnLayer);

            info.flags.OnSave(bytes);

            List<byte> poisBlock = new List<byte>();    //wi-pois

            List<byte> poiBlock = new List<byte>();     //wi-pois-poi

            foreach (PointOfInterest poi in info.pointsOfInterest)
            {
                int lastIndex = poiBlock.Count;
                poi.OnSave(poiBlock);                                       //wi-pois-poi-d

                int size = poiBlock.Count - lastIndex;

                SaveHelper.SaveInt32(poisBlock, size);                      //wi-pois-poi-h-s
                SaveHelper.SaveInt32(poisBlock, PointOfInterest.VERSION);   //wi-pois-poi-h-v
                SaveHelper.SaveBytesFlat(poisBlock, poiBlock, lastIndex, size);
            }

            List<byte> poisHeaderBlock = new List<byte>();                      //wi-pois-h
            SaveHelper.SaveInt32(poisHeaderBlock, poisBlock.Count);             //wi-pois-h-s
            SaveHelper.SaveInt32(poisHeaderBlock, info.pointsOfInterest.Count); //wi-pois-h-c

            SaveHelper.SaveBytesFlat(bytes, poisHeaderBlock.ToArray());
            SaveHelper.SaveBytesFlat(bytes, poisBlock.ToArray());

            List<byte> housingsDataBlock = new List<byte>();

            foreach (Housing housing in info.housings)
                housing.OnSave(housingsDataBlock);

            List<byte> housingsHeaderBlock = new List<byte>();
            SaveHelper.SaveInt32(housingsHeaderBlock, housingsDataBlock.Count);
            SaveHelper.SaveInt32(housingsHeaderBlock, info.housings.Count);

            SaveHelper.SaveBytesFlat(bytes, housingsHeaderBlock);
            SaveHelper.SaveBytesFlat(bytes, housingsDataBlock);

            stream.Write(bytes.ToArray());
        }

        public LoadError Load(string folderName, out WorldInfo info)
        {
            info = new WorldInfo()
            {
                time = 0,
                playerPositions = new Vector3[World.MAX_PLAYERS],
                playerLayers = new int[World.MAX_PLAYERS],
                furthestLayer = -1,
                pointsOfInterest = new List<PointOfInterest>(),
                flags = new WorldFlags(),

                housings = new List<Housing>(),
            };

            string loadName = GetLoadFileName(folderName);

            if (!File.Exists(loadName))
            {
                Console.WriteLine("Could not load world info. The file " + FILE_NAME_WINFO + EXT_WINFO + " does not exist!");
                return LoadError.FileDoesntExist;
            }

            using (FileStream fs = new FileStream(loadName, FileMode.Open, FileAccess.Read, FileShare.None, 1024))
            {
                var result = LoadFromStream(fs, out info);
                if (result != LoadError.Success)
                    return result; 
            }
            
            return LoadError.Success;
        }

        public LoadError LoadFromStream(Stream stream, out WorldInfo info)
        {
            info = new WorldInfo()
            {
                time = 0,
                playerPositions = new Vector3[World.MAX_PLAYERS],
                playerLayers = new int[World.MAX_PLAYERS],
                furthestLayer = -1,
                pointsOfInterest = new List<PointOfInterest>(),
                flags = new WorldFlags(),

                housings = new List<Housing>(),
            };

            using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, false))
            {
                int version = reader.ReadInt32();   //h-v

                if (version < MIN_VERSION)
                    return LoadError.InvalidVersion;

                float worldTime = reader.ReadSingle();
                info.time = worldTime;

                if (version == 0)
                    _ = reader.ReadInt32(); //idk why this is here, but there's a random 4-byte padding in between these for some reason.

                if (version >= 2)
                {
                    info.furthestLayer = reader.ReadInt32();
                }

                if (version >= 1)
                {
                    if (version <= 4)
                    {
                        info.playerPositions[0].X = reader.ReadSingle();
                        info.playerPositions[0].Y = reader.ReadSingle();
                        info.playerPositions[0].Z = reader.ReadSingle();
                    } 
                    else
                    {
                        for (int i = 0; i < info.playerPositions.Length; i++)
                        {
                            info.playerPositions[i].X = reader.ReadSingle();
                            info.playerPositions[i].Y = reader.ReadSingle();
                            info.playerPositions[i].Z = reader.ReadSingle();
                            info.playerLayers[i] = reader.ReadInt32();
                        }
                        info.spawnPosition.X = reader.ReadSingle();
                        info.spawnPosition.Y = reader.ReadSingle();
                        info.spawnPosition.Z = reader.ReadSingle();
                        info.spawnLayer = reader.ReadInt32();
                    }
                }

                if (version >= 3)
                {
                    info.flags.Version = reader.ReadInt32();
                    info.flags.Size = reader.ReadInt32();
                    info.flags.Flags = (WorldFlags.FlagValues)reader.ReadInt32();
                }

                int sizePois = reader.ReadInt32();
                int numPois = reader.ReadInt32();

                byte[] poisBuffer = new byte[sizePois];
                reader.Read(poisBuffer);

                int index = 0;
                for (int i = 0; i < numPois; i++)
                {
                    int sizePoi = SaveHelper.LoadInt32(poisBuffer, ref index);
                    int versionPoi = SaveHelper.LoadInt32(poisBuffer, ref index);

                    if (versionPoi < PointOfInterest.MIN_VERSION)
                        index += sizePoi;    //skip loading the rest of this point of interest
                    else
                    {
                        PointOfInterest poi = new PointOfInterest();
                        poi.OnLoad(poisBuffer, ref index, in versionPoi);
                        info.pointsOfInterest.Add(poi);
                    }
                }

                if (version >= 4)
                {
                    //  hs: housing block
                    //      h:
                    //          s: size (int) of data block
                    //          n: number of elements
                    //      h: housing data blocks
                    //          See HousingTasker.cs for serialization info
                    int size = reader.ReadInt32();
                    int num = reader.ReadInt32();

                    byte[] bytes = new byte[size];
                    reader.Read(bytes, 0, size);

                    int offset = 0;
                    for (int i = 0; i < num; i++)
                    {
                        Housing housing = new Housing();
                        housing.OnLoad(bytes, ref offset);

                        info.housings.Add(housing);
                    }
                }
            }

            return LoadError.Success;
        }

        private string GetLoadFileName(string folderName)
        {
            string name = SAVE_FOLDER + folderName + "/" + FILE_NAME_WINFO + EXT_WINFO;
            if (File.Exists(name))
                return name;
            else return SAVE_FOLDER + folderName + "/" + FILE_NAME_WINFO_OLD + EXT_WINFO;
        }

        private string GetFullName(string folderName)
        {
            return SAVE_FOLDER + folderName + "/" + FILE_NAME_WINFO + EXT_WINFO;
        }

        public override bool HandleError(LoadError error, string folderName)
        {
            switch (error)
            {
                case LoadError.InvalidVersion:
                    Console.WriteLine("World Info file could not be loaded. The current file version ({0}) is not supported.", Version);
                    return true;
                case LoadError.FileDoesntExist:
                    Console.WriteLine("World Info file does not exist.", GetFullName(folderName));
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
