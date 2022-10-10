using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;

namespace ViMG.Entities
{
    public class EntityShrine : Entity, ICubeTracker
    {
        public CubePosition TrackedPosition { get; private set; }
        private readonly Buff buff;

        private const float COOLDOWN_TIME = 1f;//20f * 60f;  //20 minute cooldown timer
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
                world.GetChunkManager().MarkDirty(ChunkPosition.CubeChunk(TrackedPosition), false);
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
                world.GetChunkManager().MarkDirty(ChunkPosition.CubeChunk(TrackedPosition), false);
                hasUpdatedChunk = true;
                return true;
            }
            else return false;
        }

        public void TrackingCubeDestroyed(World world, ChunkManager cm)
        {
        }
    }
}
