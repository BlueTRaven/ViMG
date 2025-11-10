using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public class SessionInformation
    {
        public string LastLoadedSave;
        public string[] LoadedMods = new[] { "ModGameBase" }; // Mods to load
        public string? ModsFolder = null; // Must be ABSOLUTE
    }
}
