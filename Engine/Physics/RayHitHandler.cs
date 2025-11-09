using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using BepuPhysics;
using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace ViMG.Physics
{
    public struct RayHit
    {
        public Vector3 Normal;
        public float T;
        public CollidableReference Collidable;
        public bool Hit;
    }

    public unsafe struct RayHitHandler : IRayHitHandler
    {
        public Buffer<RayHit> Hits;
        public int* IntersectionCount;

        private readonly BodyHandle owner;

        public RayHitHandler(Buffer<RayHit> buffer, BodyHandle owner, int maxRaycasts, int* intersectionCount)
        {
            this.Hits = buffer;
            this.owner = owner;
            
            for (int i = 0; i < maxRaycasts; i++)
            {
                Hits[i] = new RayHit()
                {
                    T = float.MaxValue,
                    Hit = false
                };
            }

            this.IntersectionCount = intersectionCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable)
        {
            return collidable.Mobility == CollidableMobility.Static || owner != collidable.BodyHandle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return collidable.Mobility == CollidableMobility.Static || owner != collidable.BodyHandle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable, int childIndex)
        {
            maximumT = t;
            ref var hit = ref Hits[ray.Id];
            if (t < hit.T)
            {
                if (hit.T == float.MaxValue)
                    ++*IntersectionCount;
                hit.Normal = normal;
                hit.T = t;
                hit.Collidable = collidable;
                hit.Hit = true;
            }
        }
    }
}
