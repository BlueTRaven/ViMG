using BrUtility;
using Engine;
using Engine.Networking;
using Engine.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Entities
{
    public class AimedLightning : Entity, IHitboxOwner, ISyncedEntity
    {
        private static VerySimpleMesh mesh;
        private const float ADVANCE_TIME = 3f / 60f;

        private Vector3 advanceDirection;
        private float advanceLength;
        private float maxLength;
        private float advanceVariance;
        private int advanceNum = 0;
        private readonly HitboxManager.HitboxParameters parameters;
        private readonly HitboxManager.HitboxStats stats;

        private float advanceTimer;
        public FastList<Vector3> positions = new FastList<Vector3>();
        private FastList<Vector3> basePositions = new FastList<Vector3>();

        private bool hasTouched;
        private int hitbox = -1;

        private int seed;
        private float timer;

        public AimedLightning(Vector3 position, Vector3 advanceDirection, float advanceLength, float maxLength, float advanceVariance, HitboxManager.HitboxStats stats)
        {
            AlwaysRender = true;

            timer = 6f;

            this.Position = position;

            this.advanceDirection = advanceDirection;
            this.advanceLength = advanceLength;
            this.maxLength = maxLength;
            this.advanceVariance = advanceVariance;
            this.stats = stats;

            parameters = new HitboxManager.HitboxParameters()
            {
                owner = this,
                manager = this,
                bounds = OrientedBoundingBox.Empty,
                direction = advanceDirection,
                stats = stats,
                canInteract = true
            };
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            seed = random.Next();
        }

        public override void OnUnload()
        {
            base.OnUnload();

            if (hitbox != -1)
                world.HitboxManager.Remove(hitbox);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            timer -= (float)deltaTime;
            advanceTimer -= (float)deltaTime;

            //TODO: make this collide with world

            positions.Clear();
            basePositions.Clear();
            positions.Add(Position);
            basePositions.Add(Position);

            float totalLength = 0;
            if (advanceTimer <= 0 && advanceNum < 8)
            {
                advanceTimer = ADVANCE_TIME;

                advanceNum += 1;

                seed = random.Next();

                if (advanceNum >= 8)
                {
                    timer = float.Min(timer, 5f / 60f);
                }
            }

            for (int i = 1; i < advanceNum; i++)
            { 
                PCG32 pcg = new PCG32((ulong)(seed + i));

                Vector3 o = new Vector3(pcg.NextFloat(-advanceVariance, advanceVariance), 0, 0);
                o = Vector3.Transform(o, Matrix.CreateRotationZ(pcg.NextFloat(0, MathF.PI * 2)));
                // TODO this shouldn't use camera
                //o = Vector3.Transform(o, Matrix.CreateRotationX(-Main.camera.RotationEuler.X) * Matrix.CreateRotationY(-Main.camera.RotationEuler.Y));

                Vector3 previousPosition = basePositions[i - 1];

                float realAdvanceLength = advanceLength;

                totalLength += advanceLength;
                if (totalLength > maxLength)
                    realAdvanceLength = totalLength - maxLength;

                Vector3 advance = advanceDirection * realAdvanceLength;
                Vector3 nextPosition = previousPosition + advance;
                positions.Add(nextPosition + o);
                basePositions.Add(nextPosition);

                if (i == advanceNum - 1)
                {
                    if (hitbox == -1)
                    {
                        HitboxManager.HitboxParameters parameters = this.parameters with
                        {
                            bounds = OrientedBoundingBox.FromTwoPositions(positions[i - 1], positions[i])
                        };

                        hitbox = world.HitboxManager.Add(parameters);
                    }
                    else world.HitboxManager.Update(hitbox, OrientedBoundingBox.FromTwoPositions(positions[i - 1], positions[i]));
                }
            }

            if (timer <= 0)
                world.EntityManager.Kill(this);
        }

        public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
        {
            if (((int)us.group & HitboxManager.GROUP_SOURCE_MASK) != ((int)other.group & HitboxManager.GROUP_SOURCE_MASK) &&
                ((int)other.group & HitboxManager.DAMAGE_TYPE_TAKE) > 0 && other.canInteract)
            {
                if (hitbox != -1)
                {
                    world.HitboxManager.Remove(hitbox);
                    hitbox = -1;
                }

                hasTouched = true;
                timer = float.Min(timer, 5f / 60f);
            }
        }

        public void GetSyncedEntity(out SyncedEntity state)
        {
            state = new SyncedEntity
            {
                position = Position,
                velocity = advanceDirection,
                timers = { [0] = timer, [1] = advanceLength, [2] = maxLength, [3] = advanceVariance },
                counters = { [0] = advanceNum, [1] = seed },
            };
        }
    }
}
