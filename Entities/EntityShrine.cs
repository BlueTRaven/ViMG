using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;

namespace ViMG.Entities
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
    [EntityMeta(0)]
    public class EntityShrine : Entity, ICubeTracker
    {
        public CubePosition TrackedPosition { get; private set; }
        private Buff buff;

        private const float COOLDOWN_TIME = 20f * 60f;  //20 minute cooldown timer
        private float cooldownTimer;
        public float CooldownTimer => cooldownTimer;

        private bool hasUpdatedChunk;

        public EntityShrine()
        {
        }

        public EntityShrine(CubePosition position, Buff buff)
        {
            this.TrackedPosition = position;
            this.buff = buff;
            this.Position = position.InWorldSpace(null);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            cooldownTimer -= (float)deltaTime;

            if (cooldownTimer <= 0 && !hasUpdatedChunk)
            {
                hasUpdatedChunk = true;
                //Mark the tracked chunk as dirty; this will force the cube to change its source rectangle.
                world.ChunkManager2.MarkDirty(ChunkPosition.CubeChunk(TrackedPosition));
            }
        }

        public bool OnInteract(Player player)
        {
            if (cooldownTimer <= 0)
            {
                cooldownTimer = COOLDOWN_TIME;

                player.GetBuffManager().RemoveAllWithTag("blessing");
                player.GetBuffManager().AddBuff(new Buff.BuffInstance(buff, 5f * 60f));

                //Mark the tracked chunk as dirty; this will force the cube to change its source rectangle.
                world.ChunkManager2.MarkDirty(ChunkPosition.CubeChunk(TrackedPosition));
                hasUpdatedChunk = true;
                return true;
            }
            else return false;
        }

        public void TrackingCubeUpdated(World world, ChunkManager2 manager, ushort updatedId)
        {
        }

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

            SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);

            SaveHelper.SaveFloat32(saveBytes, cooldownTimer);

            SaveHelper.SaveString(saveBytes, buff.Identifier);
        }

        public override void OnLoad(byte[] loadBytes, in int version)
        {
            base.OnLoad(loadBytes, version);

            int index = 0;

            TrackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
            Position = TrackedPosition.InWorldSpace(null);

            cooldownTimer = SaveHelper.LoadFloat32(loadBytes, ref index);

            buff = Main.Registry.BuffRegistry.Get(SaveHelper.LoadString(loadBytes, ref index));
        }
    }
}
