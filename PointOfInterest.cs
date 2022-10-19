using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public struct PointOfInterest
    {
        public const int VERSION = 0;
        public const int MIN_VERSION = 0;

        public CubePosition position;
        public string name;
        public int weight;

        public bool valid;

        public PointOfInterest(CubePosition position, string name, int weight)
        {
            this.position = position;
            this.name = name;
            this.weight = weight;

            valid = true;
        }

        public void OnSave(List<byte> saveBytes)
        {
            SaveHelper.SaveCubePosition(saveBytes, position);
            SaveHelper.SaveString(saveBytes, name);
            SaveHelper.SaveInt32(saveBytes, weight);
        }

        public void OnLoad(byte[] loadBytes, ref int index, in int version)
        {
            position = SaveHelper.LoadCubePosition(loadBytes, ref index);
            name = SaveHelper.LoadString(loadBytes, ref index);
            weight = SaveHelper.LoadInt32(loadBytes, ref index);

            valid = true;
        }
    }
}
