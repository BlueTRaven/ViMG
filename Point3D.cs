using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public struct Point3D
    {
        public int x;
        public int y;
        public int z;

        public Point3D(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    public ref struct ValuePoint3D
    {
        public int x;
        public int y;
        public int z;

        public ValuePoint3D(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}
