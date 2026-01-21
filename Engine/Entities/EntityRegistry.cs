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
            var prev = client.Previous(1).entities.GetByRef(ref reference);
            var prevInterp = client.prevInterpState.entities.GetByRef(ref reference);
            var curr = client.Current().entities.GetByRef(ref reference);
            if (!client.Previous(1).entities.IsActive(ref reference))
                prevInterp = curr;
            if (!client.Current().entities.IsActive(ref reference))
                curr = prevInterp;

            return GetInterpolated(ref prev, ref prevInterp, ref curr, client.TimeC);
        }

        protected virtual BasicState GetInterpolated(ref readonly BasicState prev, ref readonly BasicState prevInterp, ref readonly BasicState curr, double t)
        {
            var interp = prevInterp;
            interp.position = prevInterp.GetInterpPosition(curr, t);
            interp.rotation = prevInterp.GetInterpRotation(curr, t);
            interp.velocity = prevInterp.GetInterpVelocity(curr, t);
            for (int i = 0; i < 4; i++)
                interp.timers[i] = prevInterp.GetInterpTimer(curr, i, t);

            for (int i = 0; i < 4; i++)
                interp.counters[i] = prevInterp.GetInterpCounter(curr, i, t);

            interp.aliveTime = float.Lerp(prevInterp.aliveTime, curr.aliveTime, (float)t);

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

        // If the discontinuity between a and b is larger than error, return b.
        // When a server entity sets a timer to some value, the client will naturally try to interpolate this when we don't want it to.
        // This can detect that and throw it out.
        // For this reason, don't use timers smaller than error.
        public static float NetworkLerp(float a, float interp, float b, float t, float error = 1.0f / 60.0f)
        {
            if (float.Abs(b - a) > error)
            {
                return b;
            }

            return float.Lerp(interp, b, t);
        }
    }
}
