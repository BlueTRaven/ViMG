using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Physics
{
    public struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
    {
        private CollidableProperty<PhysicsProperties> properties;

        public SpringSettings ContactSpringiness;
        public float MaximumRecoveryVelocity;
        public float FrictionCoefficient;

        public NarrowPhaseCallbacks(CollidableProperty<PhysicsProperties> properties, SpringSettings contactSpringiness, float maximumRecoveryVelocity = 2f, float frictionCoefficient = 1f)
        {
            this.properties = properties;
            ContactSpringiness = contactSpringiness;
            MaximumRecoveryVelocity = maximumRecoveryVelocity;
            FrictionCoefficient = frictionCoefficient;
        }

        public void Initialize(Simulation simulation)
        {
            properties.Initialize(simulation);

            //Use a default if the springiness value wasn't initialized... at least until struct field initializers are supported outside of previews.
            if (ContactSpringiness.AngularFrequency == 0 && ContactSpringiness.TwiceDampingRatio == 0)
            {
                ContactSpringiness = new(30, 1);
                MaximumRecoveryVelocity = Cube.CUBE_SCALE * 2f;
                FrictionCoefficient = Cube.CUBE_SCALE;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
        {
            //It's impossible for two statics to collide, and pairs are sorted such that bodies always come before statics.
            if (b.Mobility != CollidableMobility.Static)
            {
                return SubgroupCollisionFilter.AllowCollision(properties[a.BodyHandle].Filter, properties[b.BodyHandle].Filter);
            }
            return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            Vector3 nrm = Vector3.Zero;
            for (int i = 0; i < manifold.Count; i++)
            {
                nrm += manifold.GetNormal(ref manifold, i);
            }

            nrm /= manifold.Count;
            nrm = Vector3.Normalize(nrm);

            float dot = Vector3.Dot(nrm, Vector3.UnitY);

            //If the collidable is colliding with the side of something, don't use friction
            //This is not very physically correct... but whatever.
            //The reason why we're doing this is because if the player decides to walk into a wall, because of friction they essentially get stuck in it.
            if (dot < 0.5f)
                pairMaterial.FrictionCoefficient = 0;
            else 
                pairMaterial.FrictionCoefficient = FrictionCoefficient;

            pairMaterial.FrictionCoefficient *= (properties[pair.A.BodyHandle].Friction + properties[pair.A.BodyHandle].Friction) / 2f;
            pairMaterial.MaximumRecoveryVelocity = MaximumRecoveryVelocity;
            pairMaterial.SpringSettings = ContactSpringiness;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
        {
            return true;
        }

        public void Dispose()
        {
        }
    }
}
