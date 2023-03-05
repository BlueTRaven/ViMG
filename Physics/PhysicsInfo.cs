using BepuPhysics;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Physics
{
    public class PhysicsInfo
    {
        public const float SIM_GRAVITY = World.GRAVITY * 32;

        public Simulation Simulation;
        public BufferPool GlobalBufferPool;
        public CollidableProperty<PhysicsProperties> Properties;

        public PhysicsInfo()
        {
            GlobalBufferPool = new BufferPool();
            Properties = new CollidableProperty<PhysicsProperties>(GlobalBufferPool);

            Simulation = Simulation.Create(GlobalBufferPool, new NarrowPhaseCallbacks(Properties, new SpringSettings(30, 1), Cube.CUBE_SCALE * 2f, 0.8f),
                    new PoseIntegratorCallbacks(new System.Numerics.Vector3(0, SIM_GRAVITY, 0), 0.8f), new SolveDescription(8, 1));
        }
    }
}
