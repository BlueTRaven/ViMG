using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EntitySerializableAttribute : Attribute
    {
        [Flags]
        public enum SerializationType
        {
            None = 0,
            World,     //serializable by the world.
            Struct,    //serializable by structures. Any entities with this Serialization type will become serializable by the world if not already, if generated from a structure.
            Server,
            All = World | Struct,
            AllWithServer = All | Server,
        }

        public readonly SerializationType serializationType;

        public EntitySerializableAttribute(SerializationType serializationType)
        {
            this.serializationType = serializationType;
        }
    }
}
