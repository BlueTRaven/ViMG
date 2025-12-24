using Engine.Networking;
using SharpDX.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Entities
{
    public class EntityType : IRegisterable
    {
        private readonly string identifier;
        public string Identifier => identifier;

        public EntityMetaAttribute? meta;
        public EntitySerializableAttribute? serializable;

        public EntityType(string identifier, Type type)
        {
            this.identifier = identifier;

            meta = type.GetCustomAttribute<EntityMetaAttribute>();
            serializable = type.GetCustomAttribute<EntitySerializableAttribute>();
        }

        public virtual void GetEntityMeshingData(BasicState state)
        {

        }
    }

    public class EntityRegistry : ObjRegistry<EntityType>
    {
    }
}
