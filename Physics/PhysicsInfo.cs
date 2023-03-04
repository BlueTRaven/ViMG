using BepuPhysics;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Physics
{
    public class PhysicsInfo
    {
        public const float SIM_GRAVITY = World.GRAVITY * 16;

        public Simulation Simulation;
        public BufferPool GlobalBufferPool;
        public CollidableProperty<PhysicsProperties> Properties;

        public PhysicsInfo()
        {
            GlobalBufferPool = new BufferPool();
            Properties = new CollidableProperty<PhysicsProperties>(GlobalBufferPool);

            Simulation = Simulation.Create(GlobalBufferPool, new NarrowPhaseCallbacks(Properties, new SpringSettings(30, 3)),
                    new PoseIntegratorCallbacks(new System.Numerics.Vector3(0, SIM_GRAVITY, 0), angularDamping: 0.2f), new SolveDescription(8, 1));
        }
    }
}
