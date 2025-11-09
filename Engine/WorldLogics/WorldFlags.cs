using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.WorldLogics
{
    public class WorldFlags
    {
        public const int VERSION = 0;
        public const int MIN_VERSION = 0;

        [Flags]
        public enum FlagValues
        {
            NONE = 0,
            SKULLHEAD_DEAD = 1 << 0,
            MERCHANT_SPAWNED = 1 << 1,
            MERCHANT_SAVED = 1 << 2,
        }

        public int Version;
        public int Size;
        public FlagValues Flags;

        public bool HasFlag(FlagValues flag)
        {
            return (Flags & flag) == flag;
        }

        public void OnSave(List<byte> bytes)
        {
            SaveHelper.SaveInt32(bytes, VERSION);
            SaveHelper.SaveInt32(bytes, sizeof(int));

            SaveHelper.SaveInt32(bytes, (int)Flags);
        }

        public void OnLoad(byte[] bytes, ref int index)
        {
            Version = SaveHelper.LoadInt32(bytes, ref index);
            Size = SaveHelper.LoadInt32(bytes, ref index);

            Flags = (FlagValues)SaveHelper.LoadInt32(bytes, ref index);
        }
    }
}
