using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public abstract class WorldIO
    {
        public const string SAVE_FOLDER = "./saves/";
        
        public enum LoadError
        {
            Success,
            InvalidVersion,
            FileDoesntExist,
            Other
        }

        public string OtherError;

        //returns whether or not the error is fatal.
        public abstract bool HandleError(LoadError error, string folderName);
    }
}
