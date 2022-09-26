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
        protected const string FILE_NAME_SESSION = "session.ses";

        public enum LoadError
        {
            Success,
            InvalidVersion,
            FileDoesntExist,
            Other
        }

    }
}
