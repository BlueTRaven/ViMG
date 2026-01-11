using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Common
{
    public static class Reference<T>
    {
        public readonly struct ReferenceType
        {
            public readonly ushort id;
            // NOTE: negative values are always invalid.
            public readonly short generation;

            public ReferenceType(ushort id, short generation)
            {
                this.id = id;
                this.generation = generation;
            }

            public ReferenceType NextGeneration()
            {
                return new ReferenceType(id, (short)((generation + 1) % short.MaxValue));
            }

            public static ReferenceType INVALID = new ReferenceType(0, -1);

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(id);
                writer.Put(generation);
            }

            public static ReferenceType Deserialize(NetDataReader reader)
            {
                ushort id = reader.GetUShort();
                short generation = reader.GetShort();

                return new ReferenceType(id, generation);
            }
        }
    }
}
