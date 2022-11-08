using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Entities
{
    //For cubes that don't want a fully-fledged cube entity, but want a light.
    //[Serializable]
    [EntityMeta(0, 0)]
    public class CubeLight : Entity, ICubeTracker
    {
        private readonly CubePosition position;
        public CubePosition TrackedPosition => position;

        private Vector4 lightColor;
        private Vector2 lightExtents;
        private int light = -1;

        public CubeLight()
        {
        }

        public CubeLight(CubePosition position, Vector4 lightColor, Vector2 lightExtents)
        {
            this.position = position;
            Position = position.InWorldSpace(null) + new Vector3(Cube.CUBE_SCALE / 2);

            this.lightColor = lightColor;
            this.lightExtents = lightExtents;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            light = world.LightManager.Add(Position, lightExtents.X, lightExtents.Y, lightColor);
        }

        public bool OnInteract(Player player)
        {
            return false;
        }

        public void TrackingCubeDestroyed(World world, ChunkManager cm)
        {
            world.LightManager.Remove(light);
            world.EntityManager.Remove(this);
        }
    }
}
