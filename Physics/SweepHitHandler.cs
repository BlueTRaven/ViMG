using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using BepuUtilities.Memory;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Physics
{
    public unsafe struct SweepHitHandler : ISweepHitHandler
    {
        public RayHit* Hit;
        public int* IntersectionCount;

        private readonly BodyHandle owner;

        public SweepHitHandler(RayHit* hit, BodyHandle owner, int* intersectionCount)
        {
            this.Hit = hit;
            this.owner = owner;

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
        public void OnHit(ref float maximumT, float t, Vector3 hitLocation, Vector3 hitNormal, CollidableReference collidable)
        {
            maximumT = t;

            Hit->Normal = hitNormal;
            Hit->T = t;
            Hit->Collidable = collidable;
            Hit->Hit = true;

            ++*IntersectionCount;
        }

        public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
        {
        }
    }
}
