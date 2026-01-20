using Engine.Clients;
using Engine.Common;
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
        protected readonly string identifier;
        public string Identifier => identifier;

        public int Id;
        public readonly Type type;

        public EntityMetaAttribute? meta;
        public EntitySerializableAttribute? serializable;

        protected EntityType(Type type)
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

        public virtual BasicState GetInterpolated(ClientStates client, EntityManager.EntityReference reference)
        {
            var prev = client.prevInterpState.entities.GetByRef(ref reference);
            var curr = client.Current().entities.GetByRef(ref reference);
            if (!client.Previous(1).entities.IsActive(ref reference))
                prev = curr;
            if (!client.Current().entities.IsActive(ref reference))
                curr = prev;

            return GetInterpolated(ref prev, ref curr, client.TimeC);
        }

        protected virtual BasicState GetInterpolated(ref readonly BasicState a, ref readonly BasicState b, double t)
        {
            var interp = a;
            interp.position = a.GetInterpPosition(b, t);
            interp.rotation = a.GetInterpRotation(b, t);
            interp.velocity = a.GetInterpVelocity(b, t);
            for (int i = 0; i < 4; i++)
                interp.timers[i] = a.GetInterpTimer(b, i, t);

            for (int i = 0; i < 4; i++)
                interp.counters[i] = a.GetInterpCounter(b, i, t);

            interp.aliveTime = float.Lerp(a.aliveTime, b.aliveTime, (float)t);

            return interp;
        }
    }

    public class EntityRegistry : ObjRegistry<EntityType>
    {
        protected override void DoRegistration()
        {
            base.DoRegistration();

            Register(new Common.Entities.Player());
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
