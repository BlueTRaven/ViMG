using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public abstract class WorldIO
    {
        protected const string SAVE_FOLDER = "./saves/";
        
        public enum LoadError
        {
            Success,
            InvalidVersion,
            FileDoesntExist,
            Other
        }

        public string OtherError;
    }
}
