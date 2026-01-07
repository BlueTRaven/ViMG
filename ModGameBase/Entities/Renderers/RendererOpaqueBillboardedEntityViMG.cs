using BrUtility;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ModGameBase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Entities.Renderers
{
    public class RendererOpaqueBillboardedEntityViMG
    {
        private class RenderedGeneric : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            private readonly RendererDeferred.DrawMaterial material;
            private readonly Vector2 scale;
            private readonly Vector3 offset;
            private readonly Color color;
            private readonly RectangleF? sourceRect;

            public RenderedGeneric(string identifier, int entityType, RendererDeferred.DrawMaterial material, Vector2? scale = null, Vector3? offset = null, Color? color = null, RectangleF? sourceRect = null) : base(identifier, entityType, material)
            {
                this.material = material;
                this.scale = scale ?? new Vector2(1);
                this.offset = offset ?? new Vector3();
                this.color = color ?? Color.White;
                this.sourceRect = sourceRect;
            }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[1];

            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                RendererOpaqueBillboardedEntity.RenderedEntityDrawStats stats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    position = entity.position + offset,
                    scale = scale,
                    color = color,
                    sourceRect = sourceRect,
                    shouldDraw = true,
                };

                cachedStats[0] = stats;
                return cachedStats;
            }
        }

        private class RenderedSkeleton : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedSkeleton() : base("skeleton", Main.Registry.EntityRegistry.Get<Skeleton>().Id, new RendererDeferred.DrawMaterial("skeleton"))
            {
            }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                return cachedStats;
                //Skeleton skeleton = (Skeleton)entity;

                //RectangleF sourceRect = new RectangleF(0, 0, 16, 32);

                //if (skeleton.state != Skeleton.State.Active)
                //    sourceRect = new RectangleF(16, 0, 16, 32);

                //cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                //{
                //    scale = new Vector2(1, 2),
                //    sourceRect = sourceRect,
                //};
                //return cachedStats;
            }
        }

        private class RenderedSkeleton2 : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedSkeleton2() : base("skeleton2", Main.Registry.EntityRegistry.Get<Skeleton2>().Id, new RendererDeferred.DrawMaterial("skeleton"))
            {
            }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                RectangleF sourceRect = new RectangleF(0, 0, 16, 32);

                cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    scale = new Vector2(1, 2),
                    sourceRect = sourceRect,
                };
                return cachedStats;
            }
        }

        private class RenderedSkeletonBonePile : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedSkeletonBonePile() : base("skeleton_bonepile", Main.Registry.EntityRegistry.Get<SkeletonBonePile>().Id, new RendererDeferred.DrawMaterial("skeleton"))
            {
            }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                RectangleF sourceRect = new RectangleF(16, 0, 16, 32);

                cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    scale = new Vector2(1, 2),
                    sourceRect = sourceRect,
                };
                return cachedStats;
            }
        }

        private class RenderedBigSlime : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedBigSlime() : base("slime_big", Main.Registry.EntityRegistry.Get<SlimeBig>().Id, new RendererDeferred.DrawMaterial("slime")) { }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                const float minInterval = 0.65f;
                const float maxInterval = 0.85f;

                int ysrc = 0;

                float jumpTimer = entity.timers[0];
                float jumpTime = entity.timers[1];

                float interval = MathHelper.Lerp(minInterval, maxInterval, jumpTimer / jumpTime) * 2;

                if ((jumpTimer % interval) / interval < 0.5f)
                    ysrc = 16;

                bool noticed = entity.counters[1] > 0;
                cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    position = entity.position,
                    sourceRect = noticed ? new RectangleF(32, ysrc, 32, 32) : new RectangleF(0, ysrc, 32, 32),
                };

                return cachedStats;
            }
        }

        private class RenderedSlime : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedSlime() : base(typeof(Slime).FullName, Main.Registry.EntityRegistry.Get<Slime>().Id, new RendererDeferred.DrawMaterial("slime")) { }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[1];

            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                const float minInterval = 0.65f;
                const float maxInterval = 0.85f;

                int ysrc = 0;

                float jumpTimer = entity.timers[0];
                float jumpTime = entity.timers[1];

                float interval = MathHelper.Lerp(minInterval, maxInterval, jumpTimer / jumpTime) * 2;

                if ((jumpTimer % interval) / interval < 0.5f)
                    ysrc = 16;

                bool noticed = entity.counters[1] > 0;
                cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    position = entity.position,
                    sourceRect = noticed ? new RectangleF(16, ysrc, 16, 16) : new RectangleF(0, ysrc, 16, 16),
                };

                return cachedStats;
            }
        }

        private class RenderedCaveSlime : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedCaveSlime() : base("slime_cave", Main.Registry.EntityRegistry.Get<CaveSlime>().Id, new RendererDeferred.DrawMaterial("slime")) { }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                const float minInterval = 0.65f;
                const float maxInterval = 0.85f;

                int ysrc = 0;

                float jumpTimer = entity.timers[0];
                float jumpTime = entity.timers[1];

                float interval = MathHelper.Lerp(minInterval, maxInterval, jumpTimer / jumpTime) * 2;

                if ((jumpTimer % interval) / interval < 0.5f)
                    ysrc = 16;

                bool noticed = entity.counters[1] > 0;
                cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    position = entity.position,
                    sourceRect = noticed ? new RectangleF(48, ysrc, 16, 16) : new RectangleF(32, ysrc, 16, 16),
                };

                return cachedStats;
            }
        }

        private class RenderedGhost : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedGhost() : base("ghost", Main.Registry.EntityRegistry.Get<Ghost>().Id, new RendererDeferred.DrawMaterial("grave_ghost"))
            {
            }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                // TODO
                return cachedStats;
                //Ghost ghost = (Ghost)entity;

                //RectangleF sourceRect = new RectangleF(0, 0, 32, 32);

                //Vector3 velXZ = new Vector3(ghost.ai.Facing.X, 0, ghost.ai.Facing.Z);
                //velXZ.Normalize();

                //int direction = 0;
                //float facingDotCamera = Vector3.Dot(velXZ, Main.camera.ForwardYawOnly);
                //bool flipX = false;

                //if (facingDotCamera < -0.3f)
                //{
                //    direction = 2;
                //    sourceRect.y = 64;
                //}
                //else if (facingDotCamera < 0.2f)
                //{
                //    direction = 1;
                //    sourceRect.y = 32;

                //    float facing = velXZ.X * Main.camera.ForwardYawOnly.Z - velXZ.Z * Main.camera.ForwardYawOnly.X;

                //    if (facing < 0)
                //    {
                //        flipX = true;
                //    }
                //}

                //if (flipX)
                //{
                //    sourceRect.x += 32;
                //    sourceRect.width = -32;
                //}

                //AIFlierMelee.Funcs<Ghost> funcs = new AIFlierMelee.Funcs<Ghost> { ai = ghost.ai, entity = ghost };
                //if (funcs.GetState() == AIFlierMelee.State.Attack)
                //    sourceRect = new RectangleF(0, 96, 32, 32);
                //else if (funcs.GetState() == AIFlierMelee.State.AttackStun)
                //    sourceRect = new RectangleF(32, 96, 32, 32);

                //Vector3 offset = Vector3.Zero;

                //offset.Y = MathF.Sin(MathF.PI * 2 * (entity.Alive % 4f) / 4f) * Cube.CUBE_SCALE * 0.5f;

                //cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                //{
                //    position = entity.Position + offset,
                //    sourceRect = sourceRect,
                //    scale = new Vector2(2),
                //};
                //return cachedStats;
            }
        }

        private class RenderedCultist : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedCultist() : base("cultist", Main.Registry.EntityRegistry.Get<Cultist>().Id, new RendererDeferred.DrawMaterial("cultist"))
            {
            }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                RectangleF sourceRect = new RectangleF(0, 0, 19, 32);

                if (entity.state == (int)AIWalkerShooter.State.Normal)
                {
                    if (entity.velocity.Length() > Cube.CUBE_SCALE * 0.1f)
                    {
                        float animP = (entity.aliveTime % 0.75f) / 0.75f;

                        int frame = (int)(animP * 2f);

                        sourceRect = new RectangleF(22 + frame * 22, 0, 19, 32);
                    }
                }
                else if (entity.state == (int)AIWalkerShooter.State.Attack)
                {
                    sourceRect = new RectangleF(65, 0, 19, 32);
                }

                cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    sourceRect = sourceRect,
                    scale = new Vector2(19f / 16f, 32f / 16f),
                };

                return cachedStats;
            }
        }

        private class RenderedDucken : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedDucken() : base("ducken", Main.Registry.EntityRegistry.Get<Ducken>().Id, new RendererDeferred.DrawMaterial("ducken"))
            {
            }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                // TODO
                return cachedStats;
                //Ducken ducken = (Ducken)entity;

                //RectangleF sourceRect = new RectangleF(0, 0, 32, 32);

                //Vector3 velXZ = new Vector3(ducken.ai.Facing.X, 0, ducken.ai.Facing.Z);
                //velXZ.Normalize();

                //int direction = 0;
                //float facingDotCamera = Vector3.Dot(velXZ, Main.camera.ForwardYawOnly);
                //bool flipX = false;

                //if (facingDotCamera < -0.3f)
                //{
                //    direction = 2;
                //    sourceRect.y = 64;
                //}
                //else if (facingDotCamera < 0.2f)
                //{
                //    direction = 1;
                //    sourceRect.y = 32;

                //    float facing = velXZ.X * Main.camera.ForwardYawOnly.Z - velXZ.Z * Main.camera.ForwardYawOnly.X;

                //    if (facing < 0)
                //    {
                //        flipX = true;
                //    }
                //}

                //AIPassive.Funcs<Ducken> funcs = new AIPassive.Funcs<Ducken> { ai = ducken.ai, entity = ducken };
                //if (funcs.GetState() == AIPassive.State.Normal)
                //{
                //    if (ducken.ai.Velocity.Length() > Cube.CUBE_SCALE * 0.1f)
                //    {
                //        int numFrames;

                //        if (direction == 0 || direction == 2)
                //            numFrames = 4;
                //        else if (direction == 1)
                //            numFrames = 2;
                //        else numFrames = 0;

                //        float animP = (entity.Alive % 0.75f) / 0.75f;

                //        int frame = (int)(animP * numFrames);

                //        sourceRect.x += 32 * frame;

                //        if (flipX)
                //        {
                //            sourceRect.x += 32;
                //            sourceRect.width = -32;
                //        }
                //    }
                //}

                //cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                //{
                //    sourceRect = sourceRect,
                //};
                //return cachedStats;
            }
        }

        private class RenderedGhoul : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedGhoul() : base("ghoul", Main.Registry.EntityRegistry.Get<Ghoul>().Id, new RendererDeferred.DrawMaterial("ghoul"))
            {

            }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                // TODO
                return cachedStats;
                //Color color = Color.White * ghoul.GetAlpha();

                //cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                //{
                //    color = color,
                //    sourceRect = new RectangleF(0, 0, 16, 32),
                //    scale = new Vector2(1, 2),
                //};

                //return cachedStats;
            }
        }

        private class RenderedLeviathan : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedLeviathan() : base("leviathan", Main.Registry.EntityRegistry.Get<EntityLeviathan>().Id, new RendererDeferred.DrawMaterial("leviathan"))
            {
            }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                // TODO
                return cachedStats;
                //cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                //{
                //    color = Color.White * leviathan.GetAlpha(),
                //    sourceRect = new RectangleF(0, 0, 64, 64),
                //    scale = new Vector2(6),
                //};

                //return cachedStats;
            }
        }

        private class RenderedHeart : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedHeart() : base("heart", Main.Registry.EntityRegistry.Get<Heart>().Id, new RendererDeferred.DrawMaterial("heart"))
            {
            }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                // TODO
                return cachedStats;
                //float healthPercent = (float)entity.health / (float)heart.MaxHealth;

                //float interval = MathHelper.Lerp(0.25f, 2f, healthPercent);

                //float t = (entity.aliveTime % interval) / interval;

                //float s = MathF.Sin(MathF.PI * 2 * t) * 0.5f + 0.5f;

                //float scale = MathHelper.Lerp(0.75f, 1.15f, s);

                //cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                //{
                //    sourceRect = new RectangleF(0, 0, 16, 21),
                //    scale = new Vector2(scale),
                //};

                //return cachedStats;
            }
        }

        private class RenderedPlayerBubble : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedPlayerBubble() : base("player_bubble", Main.Registry.EntityRegistry.Get<PlayerBubble>().Id, new RendererDeferred.DrawMaterial("bubble"))
            {
            }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                float t0 = (entity.aliveTime % 1.75f) / 1.75f;
                float t1 = ((entity.aliveTime + 0.45f) % 2.05f) / 2.05f;
                float s0 = MathF.Sin(MathF.PI * 2 * t0) * 0.5f + 0.5f;
                float s1 = MathF.Sin(MathF.PI * 2 * t1) * 0.5f + 0.5f;

                float scaleX = MathHelper.Lerp(1f, 1.15f, s0);
                float scaleY = MathHelper.Lerp(0.95f, 1.15f, s1);

                cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    scale = new Vector2(scaleX, scaleY),
                    shouldDraw = entity.state != 1,
                    sourceRect = new RectangleF(0, 0, 64, 64),
                };

                return cachedStats;
            }
        }

        private class RenderedSnake : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedSnake() : base("snake", Main.Registry.EntityRegistry.Get<Snake>().Id, new RendererDeferred.DrawMaterial("snake"))
            {
            }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                Vector3 velXZ = new Vector3(entity.velocity.X, 0, entity.velocity.Z);
                velXZ.Normalize();

                float facingDotCamera = Vector3.Dot(velXZ, -Main.camera.Forward);

                //Facing within 45 degrees of the camera.
                bool isFacingCamera = facingDotCamera < MathHelper.ToRadians(45);

                RectangleF sourceRect = new RectangleF(0, 0, 32, 16);
                Vector2 scale = Vector2.One;

                if (isFacingCamera)
                {
                    sourceRect = new RectangleF(0, 16, 16, 16);
                }

                if (entity.state == (int)AIWalkerMelee.State.Normal)
                {
                    if (entity.velocity.Length() > Cube.CUBE_SCALE * 0.1f)
                    {
                        float animP = (entity.aliveTime % 0.75f) / 0.75f;

                        int frame = (int)(animP * 2f);

                        sourceRect.x += sourceRect.width * frame;
                    }
                }
                else if (entity.state == (int)AIWalkerMelee.State.Attack)
                {
                    scale = new Vector2(2);
                    sourceRect.y = 32;
                    sourceRect.width = 32;
                    sourceRect.height = 32;
                    const int NUM_FRAMES = 4;

                    // TODO hardcoded 0.25 - "AttackLockTime"
                    int frame = (int)((1 - (entity.timers[2] / 0.25f)) * NUM_FRAMES);

                    sourceRect.x = 32 * frame;
                }

                cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    sourceRect = sourceRect,
                    scale = scale,
                };

                return cachedStats;
            }
        }

        private class RenderedSnakeFlying : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedSnakeFlying() : base("snake_flying", Main.Registry.EntityRegistry.Get<SnakeFlying>().Id, new RendererDeferred.DrawMaterial("snake"))
            {
            }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[2];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                RectangleF sourceRectSnake = new RectangleF(0, 34, 32, 32);

                if (entity.state == (int)AIFlierMelee.State.Attack)
                {
                    const int ATT_NUM_FRAMES = 4;
                    // TODO hardcoded 0.25 - "AttackLockTime"
                    int frame = (int)((1 - (entity.timers[2] / 0.25f)) * ATT_NUM_FRAMES);
                    sourceRectSnake = new RectangleF(32 * frame, 34, 32, 32);
                }

                RectangleF sourceRectWings = new RectangleF(0, 64, 32, 32);

                const int WINGS_NUM_FRAMES = 3;
                int wingFrame = (int)((1 - ((entity.aliveTime % 0.25f) / 0.25f)) * WINGS_NUM_FRAMES);
                sourceRectWings.x = 32 * wingFrame;


                cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    sourceRect = sourceRectWings,
                    scale = new Vector2(2),
                    position = entity.position,
                };

                cachedStats[1] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    sourceRect = sourceRectSnake,
                    scale = new Vector2(2),
                    position = entity.position,
                };

                return cachedStats;
            }
        }

        private class RenderedStoneBeetle : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            private EntityHelper.DirectionalSourceRect directionalSourceRect = new EntityHelper.DirectionalSourceRect()
            {
                front = new RectangleF(0, 0, 16, 16),
                sideLeft = new RectangleF(0, 16, 16, 16),
                back = new RectangleF(0, 32, 16, 16)
            };

            public RenderedStoneBeetle() : base("stone_beetle", Main.Registry.EntityRegistry.Get<StoneBeetle>().Id, new RendererDeferred.DrawMaterial("stone_beetle"))
            {
            }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                // TODO
                return cachedStats;
                //RectangleF sourceRect = EntityHelper.GetEntityDirectionalSourceRect(beetle.ai.Facing, directionalSourceRect);

                //if (entity.state == (int)AIWalkerShooter.State.Normal)
                //{
                //    if (entity.velocity.Length() > Cube.CUBE_SCALE * 0.1f)
                //    {
                //        float animP = (entity.aliveTime % 0.75f) / 0.75f;

                //        int frame = (int)(animP * 2f);

                //        sourceRect.x += 16 * frame;
                //    }
                //}

                //if (beetle.ai.IsInRangeOfTarget)
                //{
                //    sourceRect = new RectangleF(0, 48, 16, 16);
                //}

                //cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                //{
                //    position = entity.Position,
                //    sourceRect = sourceRect,
                //};
                //return cachedStats;
            }
        }

        private class RenderedTestNPC : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedTestNPC(int type) : base(Main.Registry.EntityRegistry.Get(type).Identifier, type, new RendererDeferred.DrawMaterial(DrawHelper.WhitePixel))
            {
            }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                var drawPos = entity.position - new Vector3(0, Cube.CUBE_SCALE * 1.5f, 0);

                var color = Color.White;

                cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    color = color,
                    position = drawPos,
                    scale = new Vector2(1, 2),
                    shouldDraw = true,
                };

                return cachedStats;
            }
        }

        private class RenderedLightStressTest : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedLightStressTest() : base("light_stress_test", Main.Registry.EntityRegistry.Get<LightStressTest>().Id, new RendererDeferred.DrawMaterial("glow_node"))
            {
            }

            private RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[64];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                for (int i = 0; i < 64; i++)
                {
                    var worldTime = entity.aliveTime * LightStressTest.Speed;
                    float t = ((worldTime + 0.03f * i) % 2f) / 2f;
                    float zt = ((worldTime + 0.03f * i + 0.3f) % 2f) / 2f;

                    float x = MathF.Cos(MathF.PI * 2 * t) * LightStressTest.RADIUS_XZ;
                    float y = MathF.Sin(MathF.PI * 2 * t) * LightStressTest.RADIUS_Y;
                    float z = -MathF.Sin(MathF.PI * 2 * zt) * LightStressTest.RADIUS_XZ;

                    Vector3 lightPos = entity.position + new Vector3(x, y, z);


                    cachedStats[i].position = lightPos;
                }

                return cachedStats;
            }
        }

        private class RenderedWorm : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedWorm() : base("worm", Main.Registry.EntityRegistry.Get<Worm>().Id, new RendererDeferred.DrawMaterial("worm"))
            {
            }

            private static RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[9];
            public override RendererOpaqueBillboardedEntity.RenderedEntityDrawStats[] GetDrawStats(BasicState entity)
            {
                // TODO
                return cachedStats;
                //cachedStats[0] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                //{
                //    position = entity.position,
                //    sourceRect = new RectangleF(0, 0, 16, 16),
                //};

                //for (int i = 0; i < 8; i++)
                //{
                //    cachedStats[i + 1] = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                //    {
                //        position = worm.trainPositions[i],
                //        sourceRect = new RectangleF(16, 0, 16, 16),
                //    };
                //}

                //return cachedStats;
            }
        }

        private readonly RendererOpaqueBillboardedEntity renderer;

        public static void DoRegistration(RendererOpaqueBillboardedEntity renderer)
        {
            renderer.registry.Register(new RenderedGeneric("imp", Main.Registry.EntityRegistry.Get<Imp>().Id, new RendererDeferred.DrawMaterial("imp"), sourceRect: new RectangleF(0, 16, 16, 16)));
            renderer.registry.Register(new RenderedSkeleton());
            renderer.registry.Register(new RenderedSkeleton2());
            renderer.registry.Register(new RenderedSkeletonBonePile());
            renderer.registry.Register(new RenderedSlime());
            renderer.registry.Register(new RenderedBigSlime());
            renderer.registry.Register(new RenderedCaveSlime());
            renderer.registry.Register(new RenderedGhost());
            renderer.registry.Register(new RenderedCultist());
            renderer.registry.Register(new RenderedGeneric("salamander", Main.Registry.EntityRegistry.Get<CaveSalamander>().Id, new RendererDeferred.DrawMaterial("salamander"), sourceRect: new RectangleF(0, 0, 16, 16)));
            renderer.registry.Register(new RenderedDucken());
            renderer.registry.Register(new RenderedGhoul());
            renderer.registry.Register(new RenderedGeneric("glow_node", Main.Registry.EntityRegistry.Get<GlowNode>().Id, new RendererDeferred.DrawMaterial(Main.assetsManager.GetAsset<Texture2D>("glow_node"), emissive: DrawHelper.WhitePixel), offset: new Vector3(0, -Cube.CUBE_SCALE, 0)));
            renderer.registry.Register(new RenderedLeviathan());
            renderer.registry.Register(new RenderedHeart());
            renderer.registry.Register(new RenderedPlayerBubble());
            renderer.registry.Register(new RenderedSnake());
            renderer.registry.Register(new RenderedSnakeFlying());
            renderer.registry.Register(new RenderedStoneBeetle());
            renderer.registry.Register(new RenderedTestNPC(Main.Registry.EntityRegistry.Get<TestNPC>().Id));
            renderer.registry.Register(new RenderedTestNPC(Main.Registry.EntityRegistry.Get<Player>().Id));
            renderer.registry.Register(new RenderedLightStressTest());
            renderer.registry.Register(new RenderedWorm());
        }
    }
}
