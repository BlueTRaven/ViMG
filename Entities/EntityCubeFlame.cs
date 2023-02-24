using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Entities
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
    [EntityMeta(1)]
    public class EntityCubeFlame : Entity, ICubeTracker, IHitboxOwner
    {
        //Store time as the point in world time after which this entity will be destroyed.
        //We do it this way so that the timer technically keeps ticking even if we unload the chunk with this cube.
        //Once we re-enter and load, it will immediately kill itself if it's well past its timer.
        private float time;
        private float timer;
        public CubePosition TrackedPosition { get; private set; }
        private int light = -1;
        private int ambientLight = -1;
        private bool isShadowmapped;
        private int hitbox = -1;

        private bool needsTimeFix = false;

        public EntityCubeFlame()
        {
        }

        public EntityCubeFlame(CubePosition position, float worldTimeExpiration)
        {
            this.Position = position.InWorldSpace();
            TrackedPosition = position;

            this.time = worldTimeExpiration;
            this.timer = worldTimeExpiration;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            if (needsTimeFix)
            {
                time = world.GetTime() + timer;
                needsTimeFix = false;
            }

            if (hitbox == -1)
                hitbox = world.HitboxManager.Add(this, new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 4f), new Vector3(Cube.CUBE_SCALE / 2f)).Offset(Position + new Vector3(Cube.CUBE_SCALE / 2f)), 
                    Vector3.Up, HitboxManager.Group.NEUTRAL_DEAL, 1, 0);

            //timer -= (float)deltaTime;

            float p0 = ((world.GetTime() + time) % 0.65f) / 0.65f;
            float s0 = MathF.Sin(MathF.PI * 2 * p0) * Cube.CUBE_SCALE * 0.25f;

            BoundingSphere sphere = new BoundingSphere(Position, Cube.CUBE_SCALE * 8);

            Vector4 ambientLightColor = Color.OrangeRed.ToVector4();
            ambientLightColor.W = 0.25f;

            LightHelper.UpdateLight(world.LightManager, new LightHelper.LightInfo(Position + new Vector3(Cube.CUBE_SCALE / 2f),
                Cube.CUBE_SCALE * 4f + s0, Cube.CUBE_SCALE * 8f, Color.OrangeRed.ToVector4()), LightHelper.LightUpdateType.UpdateClean,
                sphere, ref light, ref isShadowmapped, true);

            bool dummy = false;
            LightHelper.UpdateLight(world.LightManager, new LightHelper.LightInfo(Position + new Vector3(Cube.CUBE_SCALE / 2f),
                Cube.CUBE_SCALE * 6f + s0, Cube.CUBE_SCALE * 12f, ambientLightColor, false), LightHelper.LightUpdateType.UpdateClean,
                sphere, ref ambientLight, ref dummy, false);
            /*if (!Main.camera.GetFrustum().Intersects(sphere))
            {
                if (light != -1)
                {
                    if (isShadowmapped)
                        world.LightManager.RemoveShadowmapped(light);
                    else world.LightManager.Remove(light);
                    light = -1;
                }
            }
            else
            {
                if (light == -1)
                {
                    world.LightManager.AddShadowmapped(Position + new Vector3(Cube.CUBE_SCALE / 2f), 
                        Cube.CUBE_SCALE * 4f, Cube.CUBE_SCALE * 8f, Color.OrangeRed, out light, out isShadowmapped);
                }
            }*/

            if (world.GetTime() > time)
            {
                world.EntityManager.Remove(this);

                world.ChunkManager2.ThreadedView.SetCube(TrackedPosition, 0);
            }
        }

        public bool OnInteract(Player player)
        {
            return false;
        }

        public void TrackingCubeUpdated(World world, ChunkManager2 manager, ushort updatedId)
        {
            world.EntityManager.Remove(this);
        }

        public override void OnUnload()
        {
            base.OnUnload();

            if (light != -1)
            {
                if (isShadowmapped)
                    world.LightManager.RemoveShadowmapped(light);
                else world.LightManager.Remove(light);

                light = -1;
            }

            if (ambientLight != -1)
            {
                world.LightManager.Remove(ambientLight);
            }

            if (hitbox != -1)
            {
                world.HitboxManager.Remove(hitbox);
                hitbox = -1;
            }
        }

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

            SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);

            //SaveHelper.SaveFloat32(saveBytes, timer);
            SaveHelper.SaveFloat32(saveBytes, time);
        }

        public override void OnLoad(byte[] loadBytes, in int version)
        {
            base.OnLoad(loadBytes, version);

            int index = 0;
            TrackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);

            if (version == 0)
                timer = SaveHelper.LoadFloat32(loadBytes, ref index);
            time = SaveHelper.LoadFloat32(loadBytes, ref index);

            Position = TrackedPosition.InWorldSpace();

            if (version == 0)
                needsTimeFix = true;
        }

        public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
        {
        }
    }
}
