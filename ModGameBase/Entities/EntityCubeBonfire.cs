using Engine.Common;
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
    [EntityMeta(0)]
    public class EntityCubeBonfire : Entity, ICubeTracker, IHitboxOwner
    {
        public CubePosition TrackedPosition { get; private set; }
        private int hitbox = -1;

        public EntityCubeBonfire()
        {
        }

        public EntityCubeBonfire(CubePosition position)
        {
            this.Position = position.InWorldSpace();
            TrackedPosition = position;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);
            if (hitbox == -1)
                hitbox = world.HitboxManager.Add(this, new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 4f), new Vector3(Cube.CUBE_SCALE / 2f)).Offset(Position + new Vector3(Cube.CUBE_SCALE / 2f)),
                    Vector3.Up, HitboxManager.Group.NEUTRAL_DEAL, 1, 0);

            //timer -= (float)deltaTime;

            float p0 = (world.GetTime() % 0.65f) / 0.65f;
            float s0 = MathF.Sin(MathF.PI * 2 * p0) * Cube.CUBE_SCALE * 0.25f;

            BoundingSphere sphere = new BoundingSphere(Position, Cube.CUBE_SCALE * 8);

            world.LightManager2.AddShadowmapped(new LightManager2.LightConfig
            {
                position = Position + new Vector3(Cube.CUBE_SCALE / 2f),
                min = Cube.CUBE_SCALE * 8f + s0,
                max = Cube.CUBE_SCALE * 16f,
                color = Color.OrangeRed.ToVector4(),
            });
        }

        public bool OnInteract(Player player)
        {
            return false;
        }

        public void TrackingCubeUpdated(World world, ChunkManager manager, Player? player, ushort updatedId)
        {
            world.EntityManager.Kill(this);
        }

        public override void OnUnload()
        {
            base.OnUnload();

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
        }

        public override void OnLoad(World world, byte[] loadBytes, in int version)
        {
            base.OnLoad(world, loadBytes, version);

            int index = 0;
            TrackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
            Position = TrackedPosition.InWorldSpace();
        }

        public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
        {
        }
    }
}
