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
        private class TypeStatsGeneric : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            private readonly RendererDeferred.DrawMaterial material;
            private readonly Vector2 scale;
            private readonly Vector3 offset;
            private readonly Color color;
            private readonly RectangleF? sourceRect;

            public TypeStatsGeneric(string identifier, Type entityType, RendererDeferred.DrawMaterial material, Vector2? scale = null, Vector3? offset = null, Color? color = null, RectangleF? sourceRect = null) : base(identifier, entityType, material)
            {
                this.material = material;
                this.scale = scale ?? new Vector2(1);
                this.offset = offset ?? new Vector3();
                this.color = color ?? Color.White;
                this.sourceRect = sourceRect;
            }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[1];

            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                RendererOpaqueBillboardedEntity.TypeStatsDrawStats stats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    position = entity.Position + offset,
                    scale = scale,
                    color = color,
                    sourceRect = sourceRect,
                    shouldDraw = true,
                };

                cachedStats[0] = stats;
                return cachedStats;
            }
        }

        private class TypeStatsSkeleton : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsSkeleton() : base("skeleton", typeof(Skeleton), new RendererDeferred.DrawMaterial("skeleton"))
            {
            }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[1];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Skeleton skeleton = (Skeleton)entity;

                RectangleF sourceRect = new RectangleF(0, 0, 16, 32);

                if (skeleton.state != Skeleton.State.Active)
                    sourceRect = new RectangleF(16, 0, 16, 32);

                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    scale = new Vector2(1, 2),
                    sourceRect = sourceRect,
                };
                return cachedStats;
            }
        }

        private class TypeStatsSkeleton2 : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsSkeleton2() : base("skeleton2", typeof(Skeleton2), new RendererDeferred.DrawMaterial("skeleton"))
            {
            }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[1];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Skeleton2 skeleton = (Skeleton2)entity;

                RectangleF sourceRect = new RectangleF(0, 0, 16, 32);

                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    scale = new Vector2(1, 2),
                    sourceRect = sourceRect,
                };
                return cachedStats;
            }
        }

        private class TypeStatsSkeletonBonePile : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsSkeletonBonePile() : base("skeleton_bonepile", typeof(SkeletonBonePile), new RendererDeferred.DrawMaterial("skeleton"))
            {
            }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[1];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                SkeletonBonePile skeleton = (SkeletonBonePile)entity;

                RectangleF sourceRect = new RectangleF(16, 0, 16, 32);

                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    scale = new Vector2(1, 2),
                    sourceRect = sourceRect,
                };
                return cachedStats;
            }
        }

        private class TypeStatsBigSlime : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsBigSlime() : base("slime_big", typeof(SlimeBig), new RendererDeferred.DrawMaterial("slime")) { }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[1];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                SlimeBig slime = entity as SlimeBig;

                int ysrc = 32;

                const float minInterval = 0.65f;
                const float maxInterval = 0.85f;

                float interval = MathHelper.Lerp(minInterval, maxInterval, slime.ai.JumpTimer / slime.ai.JumpTime) * 2;

                if (slime.ai.OnGround && (slime.Alive % interval) / interval < 0.5f)
                    ysrc = 64;

                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    sourceRect = slime.noticeHandler.Noticed ? new RectangleF(32, ysrc, 32, 32) : new RectangleF(0, ysrc, 32, 32),
                    scale = new Vector2(2),
                };

                return cachedStats;
            }
        }

        private class TypeStatsSlime : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsSlime() : base(typeof(Slime).FullName, typeof(Slime), new RendererDeferred.DrawMaterial("slime")) { }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[1];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                return null;
                //Slime slime = entity as Slime;

                //int ysrc = 0;

                //const float minInterval = 0.65f;
                //const float maxInterval = 0.85f;

                //float interval = MathHelper.Lerp(minInterval, maxInterval, slime.ai.JumpTimer / slime.ai.JumpTime) * 2;

                //if (slime.ai.OnGround && (slime.Alive % interval) / interval < 0.5f)
                //    ysrc = 16;

                //cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                //{
                //    sourceRect = slime.noticeHandler.Noticed ? new RectangleF(16, ysrc, 16, 16) : new RectangleF(0, ysrc, 16, 16),
                //};

                //return cachedStats;
            }

            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats2(BasicState s1, BasicState s2)
            {
                const float minInterval = 0.65f;
                const float maxInterval = 0.85f;

                int ysrc = 0;

                float jumpTimer = MathHelper.Lerp(s1.timers[0], s1.timers[0], (float)Main.TimeC);
                float jumpTime = MathHelper.Lerp(s1.timers[1], s1.timers[1], (float)Main.TimeC);

                float interval = MathHelper.Lerp(minInterval, maxInterval, jumpTimer / jumpTime) * 2;

                if ((jumpTimer % interval) / interval < 0.5f)
                    ysrc = 16;

                bool noticed = s1.GetInterpCounter(s2, 1) > 0;
                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    position = s1.GetInterpPosition(s2),
                    sourceRect = noticed ? new RectangleF(16, ysrc, 16, 16) : new RectangleF(0, ysrc, 16, 16),
                };

                return cachedStats;
            }
        }

        private class TypeStatsCaveSlime : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsCaveSlime() : base("slime_cave", typeof(CaveSlime), new RendererDeferred.DrawMaterial("slime")) { }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[1];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                CaveSlime slime = entity as CaveSlime;

                int ysrc = 0;

                const float minInterval = 0.65f;
                const float maxInterval = 0.85f;

                float interval = MathHelper.Lerp(minInterval, maxInterval, slime.ai.JumpTimer / slime.ai.JumpTime) * 2;

                if (slime.ai.OnGround && (slime.Alive % interval) / interval < 0.5f)
                    ysrc = 16;

                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    sourceRect = slime.noticeHandler.Noticed ? new RectangleF(48, ysrc, 16, 16) : new RectangleF(32, ysrc, 16, 16),
                };

                return cachedStats;
            }
        }

        private class TypeStatsGhost : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsGhost() : base("ghost", typeof(Ghost), new RendererDeferred.DrawMaterial("grave_ghost"))
            {
            }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[1];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Ghost ghost = (Ghost)entity;

                RectangleF sourceRect = new RectangleF(0, 0, 32, 32);

                Vector3 velXZ = new Vector3(ghost.ai.Facing.X, 0, ghost.ai.Facing.Z);
                velXZ.Normalize();

                int direction = 0;
                float facingDotCamera = Vector3.Dot(velXZ, Main.camera.ForwardYawOnly);
                bool flipX = false;

                if (facingDotCamera < -0.3f)
                {
                    direction = 2;
                    sourceRect.y = 64;
                }
                else if (facingDotCamera < 0.2f)
                {
                    direction = 1;
                    sourceRect.y = 32;

                    float facing = velXZ.X * Main.camera.ForwardYawOnly.Z - velXZ.Z * Main.camera.ForwardYawOnly.X;

                    if (facing < 0)
                    {
                        flipX = true;
                    }
                }

                if (flipX)
                {
                    sourceRect.x += 32;
                    sourceRect.width = -32;
                }

                AIFlierMelee.Funcs<Ghost> funcs = new AIFlierMelee.Funcs<Ghost> { ai = ghost.ai, entity = ghost };
                if (funcs.GetState() == AIFlierMelee.State.Attack)
                    sourceRect = new RectangleF(0, 96, 32, 32);
                else if (funcs.GetState() == AIFlierMelee.State.AttackStun)
                    sourceRect = new RectangleF(32, 96, 32, 32);

                Vector3 offset = Vector3.Zero;

                offset.Y = MathF.Sin(MathF.PI * 2 * (entity.Alive % 4f) / 4f) * Cube.CUBE_SCALE * 0.5f;

                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    position = entity.Position + offset,
                    sourceRect = sourceRect,
                    scale = new Vector2(2),
                };
                return cachedStats;
            }
        }

        private class TypeStatsCultist : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsCultist() : base("cultist", typeof(Cultist), new RendererDeferred.DrawMaterial("cultist"))
            {
            }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[1];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Cultist cultist = (Cultist)entity;

                RectangleF sourceRect = new RectangleF(0, 0, 19, 32);

                AIWalkerShooter.Funcs<Cultist> funcs = new AIWalkerShooter.Funcs<Cultist> { ai = cultist.ai, entity = cultist };
                if (funcs.GetState() == AIWalkerShooter.State.Normal)
                {
                    if (cultist.ai.Velocity.Length() > Cube.CUBE_SCALE * 0.1f)
                    {
                        float animP = (entity.Alive % 0.75f) / 0.75f;

                        int frame = (int)(animP * 2f);

                        sourceRect = new RectangleF(22 + frame * 22, 0, 19, 32);
                    }
                }
                else if (funcs.GetState() == AIWalkerShooter.State.Attack)
                {
                    sourceRect = new RectangleF(65, 0, 19, 32);
                }

                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    sourceRect = sourceRect,
                    scale = new Vector2(19f / 16f, 32f / 16f),
                };

                return cachedStats;
            }
        }

        private class TypeStatsDucken : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsDucken() : base("ducken", typeof(Ducken), new RendererDeferred.DrawMaterial("ducken"))
            {
            }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[1];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Ducken ducken = (Ducken)entity;

                RectangleF sourceRect = new RectangleF(0, 0, 32, 32);

                Vector3 velXZ = new Vector3(ducken.ai.Facing.X, 0, ducken.ai.Facing.Z);
                velXZ.Normalize();

                int direction = 0;
                float facingDotCamera = Vector3.Dot(velXZ, Main.camera.ForwardYawOnly);
                bool flipX = false;

                if (facingDotCamera < -0.3f)
                {
                    direction = 2;
                    sourceRect.y = 64;
                }
                else if (facingDotCamera < 0.2f)
                {
                    direction = 1;
                    sourceRect.y = 32;

                    float facing = velXZ.X * Main.camera.ForwardYawOnly.Z - velXZ.Z * Main.camera.ForwardYawOnly.X;

                    if (facing < 0)
                    {
                        flipX = true;
                    }
                }

                AIPassive.Funcs<Ducken> funcs = new AIPassive.Funcs<Ducken> { ai = ducken.ai, entity = ducken };
                if (funcs.GetState() == AIPassive.State.Normal)
                {
                    if (ducken.ai.Velocity.Length() > Cube.CUBE_SCALE * 0.1f)
                    {
                        int numFrames;

                        if (direction == 0 || direction == 2)
                            numFrames = 4;
                        else if (direction == 1)
                            numFrames = 2;
                        else numFrames = 0;

                        float animP = (entity.Alive % 0.75f) / 0.75f;

                        int frame = (int)(animP * numFrames);

                        sourceRect.x += 32 * frame;

                        if (flipX)
                        {
                            sourceRect.x += 32;
                            sourceRect.width = -32;
                        }
                    }
                }

                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    sourceRect = sourceRect,
                };
                return cachedStats;
            }
        }

        private class TypeStatsGhoul : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsGhoul() : base("ghoul", typeof(Ghoul), new RendererDeferred.DrawMaterial("ghoul"))
            {

            }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[1];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Ghoul ghoul = entity as Ghoul;
                Color color = Color.White * ghoul.GetAlpha();

                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    color = color,
                    sourceRect = new RectangleF(0, 0, 16, 32),
                    scale = new Vector2(1, 2),
                };

                return cachedStats;
            }
        }

        private class TypeStatsLeviathan : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsLeviathan() : base("leviathan", typeof(EntityLeviathan), new RendererDeferred.DrawMaterial("leviathan"))
            {
            }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[1];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                EntityLeviathan leviathan = entity as EntityLeviathan;

                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    color = Color.White * leviathan.GetAlpha(),
                    sourceRect = new RectangleF(0, 0, 64, 64),
                    scale = new Vector2(6),
                };

                return cachedStats;
            }
        }

        private class TypeStatsHeart : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsHeart() : base("heart", typeof(Heart), new RendererDeferred.DrawMaterial("heart"))
            {
            }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[1];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Heart heart = entity as Heart;
                float healthPercent = (float)heart.Health / (float)heart.MaxHealth;

                float interval = MathHelper.Lerp(0.25f, 2f, healthPercent);

                float t = (entity.Alive % interval) / interval;

                float s = MathF.Sin(MathF.PI * 2 * t) * 0.5f + 0.5f;

                float scale = MathHelper.Lerp(0.75f, 1.15f, s);

                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    sourceRect = new RectangleF(0, 0, 16, 21),
                    scale = new Vector2(scale),
                };

                return cachedStats;
            }
        }

        private class TypeStatsPlayerBubble : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsPlayerBubble() : base("player_bubble", typeof(PlayerBubble), new RendererDeferred.DrawMaterial("bubble"))
            {
            }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[1];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                float t0 = (entity.Alive % 1.75f) / 1.75f;
                float t1 = ((entity.Alive + 0.45f) % 2.05f) / 2.05f;
                float s0 = MathF.Sin(MathF.PI * 2 * t0) * 0.5f + 0.5f;
                float s1 = MathF.Sin(MathF.PI * 2 * t1) * 0.5f + 0.5f;

                float scaleX = MathHelper.Lerp(1f, 1.15f, s0);
                float scaleY = MathHelper.Lerp(0.95f, 1.15f, s1);

                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    scale = new Vector2(scaleX, scaleY),
                    shouldDraw = !(entity as PlayerBubble).exploding,
                    sourceRect = new RectangleF(0, 0, 64, 64),
                };

                return cachedStats;
            }
        }

        private class TypeStatsSnake : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsSnake() : base("snake", typeof(Snake), new RendererDeferred.DrawMaterial("snake"))
            {
            }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[1];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Snake snake = entity as Snake;

                Vector3 velXZ = new Vector3(snake.ai.Velocity.X, 0, snake.ai.Velocity.Z);
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

                var funcs = new AIWalkerMelee.Funcs<Snake> { ai = snake.ai, entity = snake };
                if (funcs.GetState() == AIWalkerMelee.State.Normal)
                {
                    if (snake.ai.Velocity.Length() > Cube.CUBE_SCALE * 0.1f)
                    {
                        float animP = (entity.Alive % 0.75f) / 0.75f;

                        int frame = (int)(animP * 2f);

                        sourceRect.x += sourceRect.width * frame;
                    }
                }
                else if (funcs.GetState() == AIWalkerMelee.State.Attack)
                {
                    scale = new Vector2(2);
                    sourceRect.y = 32;
                    sourceRect.width = 32;
                    sourceRect.height = 32;
                    const int NUM_FRAMES = 4;

                    int frame = (int)((1 - (snake.ai.AttackTimer / snake.ai.AttackLockTime)) * NUM_FRAMES);

                    sourceRect.x = 32 * frame;
                }

                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    sourceRect = sourceRect,
                    scale = scale,
                };

                return cachedStats;
            }
        }

        private class TypeStatsSnakeFlying : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsSnakeFlying() : base("snake_flying", typeof(SnakeFlying), new RendererDeferred.DrawMaterial("snake"))
            {
            }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[2];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                SnakeFlying snake = entity as SnakeFlying;

                RectangleF sourceRectSnake = new RectangleF(0, 34, 32, 32);

                AIFlierMelee.Funcs<SnakeFlying> funcs = new AIFlierMelee.Funcs<SnakeFlying> { ai = snake.ai, entity = snake };
                if (funcs.GetState() == AIFlierMelee.State.Attack)
                {
                    const int ATT_NUM_FRAMES = 4;
                    int frame = (int)((1 - (snake.ai.AttackTimer / snake.ai.AttackLockTime)) * ATT_NUM_FRAMES);
                    sourceRectSnake = new RectangleF(32 * frame, 34, 32, 32);
                }

                RectangleF sourceRectWings = new RectangleF(0, 64, 32, 32);

                const int WINGS_NUM_FRAMES = 3;
                int wingFrame = (int)((1 - ((entity.Alive % 0.25f) / 0.25f)) * WINGS_NUM_FRAMES);
                sourceRectWings.x = 32 * wingFrame;


                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    sourceRect = sourceRectWings,
                    scale = new Vector2(2),
                    position = entity.Position,
                };

                cachedStats[1] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    sourceRect = sourceRectSnake,
                    scale = new Vector2(2),
                    position = entity.Position,
                };

                return cachedStats;
            }
        }

        private class TypeStatsStoneBeetle : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            private EntityHelper.DirectionalSourceRect directionalSourceRect = new EntityHelper.DirectionalSourceRect()
            {
                front = new RectangleF(0, 0, 16, 16),
                sideLeft = new RectangleF(0, 16, 16, 16),
                back = new RectangleF(0, 32, 16, 16)
            };

            public TypeStatsStoneBeetle() : base("stone_beetle", typeof(StoneBeetle), new RendererDeferred.DrawMaterial("stone_beetle"))
            {
            }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[1];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                StoneBeetle beetle = entity as StoneBeetle;

                RectangleF sourceRect = EntityHelper.GetEntityDirectionalSourceRect(beetle.ai.Facing, directionalSourceRect);

                AIWalkerShooter.Funcs<StoneBeetle> funcs = new AIWalkerShooter.Funcs<StoneBeetle> { ai = beetle.ai, entity = beetle };
                if (funcs.GetState() == AIWalkerShooter.State.Normal)
                {
                    if (beetle.ai.Velocity.Length() > Cube.CUBE_SCALE * 0.1f)
                    {
                        float animP = (entity.Alive % 0.75f) / 0.75f;

                        int frame = (int)(animP * 2f);

                        sourceRect.x += 16 * frame;
                    }
                }

                if (beetle.ai.IsInRangeOfTarget)
                {
                    sourceRect = new RectangleF(0, 48, 16, 16);
                }

                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    position = entity.Position,
                    sourceRect = sourceRect,
                };
                return cachedStats;
            }
        }

        private class TypeStatsTestNPC : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsTestNPC(Type type) : base(type.FullName, type, new RendererDeferred.DrawMaterial(DrawHelper.WhitePixel))
            {
            }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[1];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                var drawPos = entity.Position - new Vector3(0, Cube.CUBE_SCALE * 1.5f, 0);

                var color = Color.White;

                if (entity is Player player && player.IsLocalPlayer)
                {
                    var distFromCam = (Main.camera.Position - drawPos).Length();
                    if (distFromCam < Cube.CUBE_SCALE * 2f)
                    {
                        var min = Cube.CUBE_SCALE * 1.5f;
                        var max = Cube.CUBE_SCALE * 2f;
                        color *= (distFromCam - min) / (max - min);
                    }
                }

                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    color = color,
                    position = drawPos,
                    scale = new Vector2(1, 2),
                    shouldDraw = true,
                };

                return cachedStats;
            }
        }

        private class TypeStatsLightStressTest : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsLightStressTest() : base("light_stress_test", typeof(LightStressTest), new RendererDeferred.DrawMaterial("glow_node"))
            {
            }

            private RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[64];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                for (int i = 0; i < 64; i++)
                {
                    var worldTime = entity.world.GetTime() * LightStressTest.Speed;
                    float t = ((worldTime + 0.03f * i) % 2f) / 2f;
                    float zt = ((worldTime + 0.03f * i + 0.3f) % 2f) / 2f;

                    float x = MathF.Cos(MathF.PI * 2 * t) * LightStressTest.RADIUS_XZ;
                    float y = MathF.Sin(MathF.PI * 2 * t) * LightStressTest.RADIUS_Y;
                    float z = -MathF.Sin(MathF.PI * 2 * zt) * LightStressTest.RADIUS_XZ;

                    Vector3 lightPos = entity.Position + new Vector3(x, y, z);


                    cachedStats[i].position = lightPos;
                }

                return cachedStats;
            }
        }

        private class TypeStatsWorm : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public TypeStatsWorm() : base("worm", typeof(Worm), new RendererDeferred.DrawMaterial("worm"))
            {
            }

            private static RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] cachedStats = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats[9];
            public override RendererOpaqueBillboardedEntity.TypeStatsDrawStats[] GetDrawStats(Entity entity)
            {
                Worm worm = entity as Worm;

                cachedStats[0] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                {
                    position = entity.Position,
                    sourceRect = new RectangleF(0, 0, 16, 16),
                };

                for (int i = 0; i < 8; i++)
                {
                    cachedStats[i + 1] = new RendererOpaqueBillboardedEntity.TypeStatsDrawStats
                    {
                        position = worm.trainPositions[i],
                        sourceRect = new RectangleF(16, 0, 16, 16),
                    };
                }

                return cachedStats;
            }
        }

        private readonly RendererOpaqueBillboardedEntity renderer;

        public static void DoRegistration(RendererOpaqueBillboardedEntity renderer)
        {
            renderer.registry.Register(new TypeStatsGeneric("imp", typeof(Imp), new RendererDeferred.DrawMaterial("imp"), sourceRect: new RectangleF(0, 16, 16, 16)));
            renderer.registry.Register(new TypeStatsSkeleton());
            renderer.registry.Register(new TypeStatsSkeleton2());
            renderer.registry.Register(new TypeStatsSkeletonBonePile());
            renderer.registry.Register(new TypeStatsSlime());
            renderer.registry.Register(new TypeStatsBigSlime());
            renderer.registry.Register(new TypeStatsCaveSlime());
            renderer.registry.Register(new TypeStatsGhost());
            renderer.registry.Register(new TypeStatsCultist());
            renderer.registry.Register(new TypeStatsGeneric("salamander", typeof(CaveSalamander), new RendererDeferred.DrawMaterial("salamander"), sourceRect: new RectangleF(0, 0, 16, 16)));
            renderer.registry.Register(new TypeStatsDucken());
            renderer.registry.Register(new TypeStatsGhoul());
            renderer.registry.Register(new TypeStatsGeneric("glow_node", typeof(GlowNode), new RendererDeferred.DrawMaterial(Main.assetsManager.GetAsset<Texture2D>("glow_node"), emissive: DrawHelper.WhitePixel), offset: new Vector3(0, -Cube.CUBE_SCALE, 0)));
            renderer.registry.Register(new TypeStatsLeviathan());
            renderer.registry.Register(new TypeStatsHeart());
            renderer.registry.Register(new TypeStatsPlayerBubble());
            renderer.registry.Register(new TypeStatsSnake());
            renderer.registry.Register(new TypeStatsSnakeFlying());
            renderer.registry.Register(new TypeStatsStoneBeetle());
            renderer.registry.Register(new TypeStatsTestNPC(typeof(TestNPC)));
            renderer.registry.Register(new TypeStatsTestNPC(typeof(Player)));
            renderer.registry.Register(new TypeStatsLightStressTest());
            renderer.registry.Register(new TypeStatsWorm());
        }
    }
}
