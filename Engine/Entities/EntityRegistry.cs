using Engine.Networking;
using SharpDX.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;

namespace Engine.Entities
{
    public class EntityType : IRegisterable
    {
        private readonly string identifier;
        public string Identifier => identifier;

        public int Id;
        public readonly Type type;

        public EntityMetaAttribute? meta;
        public EntitySerializableAttribute? serializable;

        private EntityType(Type type)
        {
            this.identifier = type.FullName;
            this.type = type;

            meta = type.GetCustomAttribute<EntityMetaAttribute>();
            serializable = type.GetCustomAttribute<EntitySerializableAttribute>();
        }

        public static EntityType New<T>() where T : Entity
        {
            return new EntityType(typeof(T));
        }
    }

    public class EntityRegistry : ObjRegistry<EntityType>
    {
        protected override void DoRegistration()
        {
            base.DoRegistration();

            Register(EntityType.New<Player>());
            Register(EntityType.New<GenericExplosion>());
            Register(EntityType.New<Line>());
            Register(EntityType.New<EntityItem>());
        }

        public override void Register(EntityType obj)
        {
            obj.Id = Count + 1;
            base.Register(obj);
        }

        public EntityType Get<T>() where T : Entity
        {
            return Get(typeof(T).FullName);
        }

        public EntityType GetFromEntity(Entity ent)
        {
            return Get(ent.GetType().FullName);
        }
    }
}
