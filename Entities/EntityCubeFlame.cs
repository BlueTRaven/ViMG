using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Entities
{
    [Serializable]
    [EntityMeta(0)]
    public class EntityCubeFlame : Entity, ICubeTracker
    {
        private float time;
        private float timer;
        public CubePosition TrackedPosition { get; private set; }
        private int light = -1;

        public EntityCubeFlame()
        {
        }

        public EntityCubeFlame(CubePosition position, float timer)
        {
            this.Position = position.InWorldSpace(null);
            TrackedPosition = position;

            this.time = timer;
            this.timer = timer;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            timer -= (float)deltaTime;

            BoundingSphere sphere = new BoundingSphere(Position, Cube.CUBE_SCALE * 8);

            if (!Main.camera.GetFrustum().Intersects(sphere))
            {
                if (light != -1)
                {
                    world.LightManager.Remove(light);
                    light = -1;
                }
            }
            else
            {
                if (light == -1)
                {
                    light = world.LightManager.Add(Position + new Vector3(Cube.CUBE_SCALE / 2f), Cube.CUBE_SCALE * 4f, Cube.CUBE_SCALE * 8f, Color.OrangeRed);
                }
            }

            if (timer <= 0)
            {
                world.EntityManager.Remove(this);

                world.GetChunkManager().GetChunk(TrackedPosition).GetData().SetCube(TrackedPosition, 0);

                if (light != -1)
                {
                    world.LightManager.Remove(light);

                    light = -1;
                }
            }
        }

        public bool OnInteract(Player player)
        {
            return false;
        }

        public void TrackingCubeDestroyed(World world, ChunkManager cm)
        {
            world.EntityManager.Remove(this);

            if (light != -1)
            {
                world.LightManager.Remove(light);

                light = -1;
            }
        }

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

            SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);
            SaveHelper.SaveFloat32(saveBytes, timer);
            SaveHelper.SaveFloat32(saveBytes, time);
        }

        public override void OnLoad(byte[] loadBytes, in int version)
        {
            base.OnLoad(loadBytes, version);

            int index = 0;
            TrackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
            timer = SaveHelper.LoadFloat32(loadBytes, ref index);
            time = SaveHelper.LoadFloat32(loadBytes, ref index);

            Position = TrackedPosition.InWorldSpace(null);
        }
    }
}
