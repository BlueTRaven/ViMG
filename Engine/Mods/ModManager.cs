using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Mods
{
    public class ModManager
    {
        private List<Assembly> loadedAssemblies = new List<Assembly>();

        public void LoadModDlls()
        {
            foreach (string str in GlobalState.SessionInformation.LoadedMods)
            {
                string? modsFolder = GlobalState.SessionInformation.ModsFolder;

                if (modsFolder == null)
                {
                    string basePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

                    string relativePath = string.Format("../mods/{0}", "net9.0-windows7.0");
                    modsFolder = Path.Combine(basePath, relativePath);
                }
                loadedAssemblies.Add(Assembly.LoadFile(string.Format("{0}/{1}.dll", modsFolder, str)));
            }
        }
    }
}
