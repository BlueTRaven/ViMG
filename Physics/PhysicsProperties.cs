using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Physics
{
    public struct PhysicsProperties
    {
        public readonly SubgroupCollisionFilter Filter;
        public readonly float Friction;
        public Vector3 CustomGravityDirection = Vector3.Down;

        public PhysicsProperties(SubgroupCollisionFilter filter, float friction = 1f)
        {
            this.Filter = filter;
            this.Friction = friction;
        }
    }
}
