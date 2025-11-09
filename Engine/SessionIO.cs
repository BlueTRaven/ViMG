using Microsoft.Xna.Framework;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public class SessionIO : WorldIO
    {
        public const string FILE_NAME_SESSION = "session";
        public const string EXT_SESSION = ".ses";

        private const int VERSION = 0;
        private const int MIN_VERSION = 0;

        public int Version;

        public void Save()
        {
            if (!Directory.Exists(SAVE_FOLDER))
                Directory.CreateDirectory(SAVE_FOLDER);

            using (FileStream fs = new FileStream(SAVE_FOLDER + FILE_NAME_SESSION + EXT_SESSION, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine("save " + Main.SessionInformation.LastLoadedSave);
                    sw.WriteLine("loaded_mods " + string.Join(',', Main.SessionInformation.LoadedMods));
                    Options.OnSave(sw);
                }
            }
        }

        public void Load(GraphicsDeviceManager graphics)
        {
            if (!File.Exists(SAVE_FOLDER + FILE_NAME_SESSION + EXT_SESSION))
                return;

            using (FileStream fs = new FileStream(SAVE_FOLDER + FILE_NAME_SESSION + EXT_SESSION, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (StreamReader reader = new StreamReader(fs))
                {
                    //Main.SessionInformation.LastLoadedSave = reader.ReadLine().Substring(5);

                    List<string> lines = new List<string>();
                    string? line = reader.ReadLine();
                    while (line != null)
                    {
                        lines.Add(line);
                        line = reader.ReadLine();
                    }

                    foreach (string l in lines)
                    {
                        string[] split = l.Split(' ');
                        if (split[0] == "save")
                        {
                            Main.SessionInformation.LastLoadedSave = split[1];
                        } 
                        else if (split[0] == "loaded_mods")
                        {
                            Main.SessionInformation.LoadedMods = split[1].Split(',');
                        }
                    }

                    Options.OnLoad(lines, graphics);
                }
            }
        }

        public override bool HandleError(LoadError error, string folderName)
        {
            return false;
        }
    }
}
