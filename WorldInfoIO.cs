using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public class WorldInfoIO : WorldIO
    {
        public const string FILE_NAME_WINFO = "world";
        public const string EXT_WINFO = ".vis";

        private const int VERSION = 0;
        private const int MIN_VERSION = 0;

        public int Version;

        public WorldInfoIO()
        {
        }

        public void Save(string folderName, World world, List<PointOfInterest> pointsOfInterest)
        {
            //h: header block
            //  v: version (int) version of worldinfo file
            //wi: worldinfo block
            //  t: world time (float)
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

            if (!Directory.Exists(SAVE_FOLDER + folderName))
                Directory.CreateDirectory(SAVE_FOLDER + folderName);

            //FileStream is probably unnecessary since we're saving the everything all at once
            using (FileStream fs = new FileStream(GetFullName(folderName), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                fs.Write(BitConverter.GetBytes(VERSION));

                List<byte> bytes = new List<byte>();

                SaveHelper.SaveInt32(bytes, VERSION); //h-v

                SaveHelper.SaveFloat32(bytes, world.GetTime()); //wi-t

                List<byte> poisBlock = new List<byte>();    //wi-pois

                List<byte> poiBlock = new List<byte>();     //wi-pois-poi

                foreach (PointOfInterest poi in pointsOfInterest)
                {
                    int lastIndex = poiBlock.Count;
                    poi.OnSave(poiBlock);                                       //wi-pois-poi-d

                    int size = poiBlock.Count - lastIndex;

                    SaveHelper.SaveInt32(poisBlock, size);                      //wi-pois-poi-h-s
                    SaveHelper.SaveInt32(poisBlock, PointOfInterest.VERSION);   //wi-pois-poi-h-v
                    SaveHelper.SaveBytesFlat(poisBlock, poiBlock, lastIndex, size);
                }

                List<byte> poisHeaderBlock = new List<byte>();                  //wi-pois-h
                SaveHelper.SaveInt32(poisHeaderBlock, poisBlock.Count);         //wi-pois-h-s
                SaveHelper.SaveInt32(poisHeaderBlock, pointsOfInterest.Count);  //wi-pois-h-c

                SaveHelper.SaveBytesFlat(bytes, poisHeaderBlock.ToArray());
                SaveHelper.SaveBytesFlat(bytes, poisBlock.ToArray());
                
                fs.Write(bytes.ToArray());
            }
        }

        public LoadError Load(string folderName, World world, List<PointOfInterest> pointsOfInterest)
        {
            if (!File.Exists(GetFullName(folderName)))
            {
                Console.WriteLine("Could not load world info. The file world.vis does not exist!");
                return LoadError.FileDoesntExist;
            }

            using (FileStream fs = new FileStream(GetFullName(folderName), FileMode.Open, FileAccess.Read, FileShare.None, 1024))
            {
                using (BinaryReader reader = new BinaryReader(fs, Encoding.ASCII, false))
                {
                    int version = reader.ReadInt32();   //h-v

                    if (version < MIN_VERSION)
                        return LoadError.InvalidVersion;

                    float worldTime = reader.ReadSingle();
                    world.SetTime(worldTime);

                    _ = reader.ReadInt32(); //idk why this is here, but there's a random 4-byte padding in between these for some reason.

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
                            pointsOfInterest.Add(poi);
                        }
                    }
                }
            }
            
            return LoadError.Success;
        }

        private string GetFullName(string folderName)
        {
            return SAVE_FOLDER + folderName + "/" + FILE_NAME_WINFO + EXT_WINFO;
        }
    }
}
