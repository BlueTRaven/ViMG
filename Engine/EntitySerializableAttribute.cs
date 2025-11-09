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
        public enum SerializationType
        {
            None,
            World = 1 << 0,     //serializable by the world.
            Struct = 1 << 1,    //serializable by structures. Any entities with this Serialization type will become serializable by the world if not already, if generated from a structure.
            All,
        }

        public readonly SerializationType serializationType;

        public EntitySerializableAttribute(SerializationType serializationType)
        {
            this.serializationType = serializationType;
        }
    }
}
