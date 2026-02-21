using BrUtility;
using Engine;
using Engine.Clients;
using Engine.Common;
using Engine.Entities.Renderers;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ModGameBase.Entities;
using SharpDX.Direct3D9;
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

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                RendererOpaqueBillboardedEntity.RenderedEntityDrawStats stats = new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    position = entity.position + offset,
                    scale = scale,
                    color = color,
                    sourceRect = sourceRect,
                    shouldDraw = true,
                };

                renderedEntityStats.Add(stats);
            }
        }

        private class RenderedImp : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedImp() : base("imp", GlobalState.Registry.EntityRegistry.Get<Imp>().Id, new RendererDeferred.DrawMaterial("imp"))
            {
            }

            public override void OnRender(ClientStates client, ref readonly BasicState entity)
            {
                base.OnRender(client, in entity);

                float p0 = (entity.aliveTime % 0.65f) / 0.65f;
                float s0 = MathF.Sin(MathF.PI * 2 * p0) * Cube.CUBE_SCALE * 1.25f;

                client.LightManager.AddShadowmapped(new Engine.Common.LightManager2.LightConfig
                {
                    position = entity.position + new Vector3(Cube.CUBE_SCALE / 2f),
                    min = Cube.CUBE_SCALE * 4f + s0,
                    max = Cube.CUBE_SCALE * 8f,
                    color = Color.OrangeRed.ToVector4(),
                });
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                RectangleF sourceRect = new RectangleF(0, 16, 16, 16);

                Color c = EntityRendererHelper.GetHurtColor(entity, 2);
                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    scale = new Vector2(1, 1.5f),
                    sourceRect = sourceRect,
                    color = c,
                });
            }
        }

        [Obsolete()]
        private class RenderedSkeleton : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedSkeleton() : base("skeleton", GlobalState.Registry.EntityRegistry.Get<Skeleton>().Id, new RendererDeferred.DrawMaterial("skeleton"))
            {
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
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
            public RenderedSkeleton2() : base("skeleton2", GlobalState.Registry.EntityRegistry.Get<Skeleton2>().Id, new RendererDeferred.DrawMaterial("skeleton"))
            {
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                RectangleF sourceRect = new RectangleF(0, 0, 16, 32);

                Color c = EntityRendererHelper.GetHurtColor(entity, 3);

                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    scale = new Vector2(1, 2),
                    sourceRect = sourceRect,
                    color = c,
                });
            }
        }

        private class RenderedSkeletonBonePile : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedSkeletonBonePile() : base("skeleton_bonepile", GlobalState.Registry.EntityRegistry.Get<SkeletonBonePile>().Id, new RendererDeferred.DrawMaterial("skeleton"))
            {
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                RectangleF sourceRect = new RectangleF(16, 0, 16, 32);

                Color c =  EntityRendererHelper.GetHurtColor(entity, 0);
                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    scale = new Vector2(1, 2),
                    sourceRect = sourceRect,
                    color = c,
                });
            }
        }

        private class RenderedBigSlime : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedBigSlime() : base("slime_big", GlobalState.Registry.EntityRegistry.Get<SlimeBig>().Id, new RendererDeferred.DrawMaterial("slime")) { }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                const float minInterval = 0.65f;
                const float maxInterval = 0.85f;

                int ysrc = 32;

                float jumpTimer = entity.timers[0];
                float jumpTime = entity.timers[1];

                float interval = MathHelper.Lerp(minInterval, maxInterval, jumpTimer / jumpTime) * 2;

                if ((jumpTimer % interval) / interval < 0.5f)
                    ysrc += 32;

                bool noticed = entity.counters[1] > 0;

                Color c = EntityRendererHelper.GetHurtColor(entity, 2);

                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    position = entity.position,
                    scale = new Vector2(2),
                    sourceRect = noticed ? new RectangleF(32, ysrc, 32, 32) : new RectangleF(0, ysrc, 32, 32),
                    color = c,
                });
            }
        }

        private class RenderedSlime : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedSlime() : base(typeof(Slime).FullName, GlobalState.Registry.EntityRegistry.Get<Slime>().Id, new RendererDeferred.DrawMaterial("slime")) { }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                const float minInterval = 0.65f;
                const float maxInterval = 0.85f;

                int ysrc = 0;

                float jumpTimer = entity.timers[0];
                float jumpTime = entity.timers[1];

                float interval = MathHelper.Lerp(minInterval, maxInterval, jumpTimer / jumpTime) * 2;

                if ((entity.aliveTime % interval) / interval < 0.5f)
                    ysrc = 16;

                bool noticed = entity.counters[1] > 0;
                Color c = EntityRendererHelper.GetHurtColor(entity, 0);

                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    position = entity.position,
                    sourceRect = noticed ? new RectangleF(16, ysrc, 16, 16) : new RectangleF(0, ysrc, 16, 16),
                    color = c,
                });
            }
        }

        private class RenderedCaveSlime : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedCaveSlime() : base("slime_cave", GlobalState.Registry.EntityRegistry.Get<CaveSlime>().Id, new RendererDeferred.DrawMaterial("slime")) { }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
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
                Color c = EntityRendererHelper.GetHurtColor(entity, 0);

                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    position = entity.position,
                    sourceRect = noticed ? new RectangleF(48, ysrc, 16, 16) : new RectangleF(32, ysrc, 16, 16),
                    color = c,
                });
            }
        }

        private class RenderedGhost : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            private EntityHelper.DirectionalSourceRect directionalSourceRect = new()
            {
                front = new RectangleF(0, 0, 32, 32),
                sideRight = new RectangleF(0, 32, 32, 32),
                back = new RectangleF(0, 64, 32, 32)
            };

            public RenderedGhost() : base("ghost", GlobalState.Registry.EntityRegistry.Get<Ghost>().Id, new RendererDeferred.DrawMaterial("grave_ghost"))
            {
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                // TODO
                var side = EntityHelper.GetEntityDirectionalSide(client.currInterpState.camera, Vector3.Transform(Vector3.Forward, entity.rotation), directionalSourceRect);
                RectangleF sourceRect = EntityHelper.GetEntityDirectionalSourceRect(side, directionalSourceRect);

                if ((AIFlierMelee.State)entity.state == AIFlierMelee.State.Attack)
                    sourceRect = new RectangleF(0, 96, 32, 32);
                else if ((AIFlierMelee.State)entity.state == AIFlierMelee.State.AttackStun)
                    sourceRect = new RectangleF(32, 96, 32, 32);

                Vector3 offset = Vector3.Zero;

                offset.Y = MathF.Sin(MathF.PI * 2 * (entity.aliveTime % 4f) / 4f) * Cube.CUBE_SCALE * 0.5f;

                Color c = EntityRendererHelper.GetHurtColor(entity, AIWalkerMelee.INVULN_TIMER_INDEX);
                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    position = entity.position + offset,
                    sourceRect = sourceRect,
                    scale = new Vector2(2),
                    color = c,
                });
            }
        }

        private class RenderedCultist : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            private EntityHelper.DirectionalSourceRect directionalSourceRect = new()
            {
                front = new RectangleF(0, 0, 22, 32),
                sideRight = new RectangleF(0, 32, 22, 32),
                back = new RectangleF(0, 64, 22, 32)
            };

            public RenderedCultist() : base("cultist", GlobalState.Registry.EntityRegistry.Get<Cultist>().Id, new RendererDeferred.DrawMaterial("cultist"))
            {
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                var side = EntityHelper.GetEntityDirectionalSide(client.currInterpState.camera, Vector3.Transform(Vector3.Forward, entity.rotation), directionalSourceRect);
                RectangleF sourceRect = EntityHelper.GetEntityDirectionalSourceRect(side, directionalSourceRect);

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
                    sourceRect.x = 65;
                }

                Color c = EntityRendererHelper.GetHurtColor(entity, AIWalkerShooter.ATTACK_TIMER_INDEX);

                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    position = entity.position,
                    sourceRect = sourceRect,
                    scale = new Vector2(19f / 16f, 32f / 16f),
                    color = c,
                });
            }
        }

        private class RenderedDucken : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            private EntityHelper.DirectionalSourceRect directionalSourceRect = new()
            {
                front = new RectangleF(0, 0, 32, 32),
                sideRight = new RectangleF(0, 32, 32, 32),
                back = new RectangleF(0, 64, 32, 32)
            };

            public RenderedDucken() : base("ducken", GlobalState.Registry.EntityRegistry.Get<Ducken>().Id, new RendererDeferred.DrawMaterial("ducken"))
            {
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                Vector2 scale = Vector2.One;

                var side = EntityHelper.GetEntityDirectionalSide(client.currInterpState.camera, Vector3.Transform(Vector3.Forward, entity.rotation), directionalSourceRect);
                RectangleF sourceRect = EntityHelper.GetEntityDirectionalSourceRect(side, directionalSourceRect);

                if ((AIPassive.State)entity.state == AIPassive.State.Normal) 
                {
                    if (entity.velocity.Length() > Cube.CUBE_SCALE * 0.1f)
                    {
                        int numFrames;

                        if (side == EntityHelper.DirectionalSide.Front || side == EntityHelper.DirectionalSide.Back)
                            numFrames = 4;
                        else if (side == EntityHelper.DirectionalSide.Left || side == EntityHelper.DirectionalSide.Right)
                            numFrames = 2;
                        else numFrames = 0;

                        float animP = (entity.aliveTime % 0.75f) / 0.75f;

                        int frame = (int)(animP * numFrames);

                        sourceRect.x += 32 * frame;
                    }
                }

                Color c = EntityRendererHelper.GetHurtColor(entity, 3);
                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    position = entity.position,
                    sourceRect = sourceRect,
                    scale = new Vector2(2),
                    color = c,
                });
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
            public RenderedGhoul() : base("ghoul", GlobalState.Registry.EntityRegistry.Get<Ghoul>().Id, new RendererDeferred.DrawMaterial("ghoul"))
            {

            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                // TODO

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

        private class RenderedGlowNode : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedGlowNode() : base("glow_node", GlobalState.Registry.EntityRegistry.Get<GlowNode>().Id, new RendererDeferred.DrawMaterial("glow_node"))
            {
                Material.Emissive = DrawHelper.WhitePixel;
            }

            public override void OnRender(ClientStates client, ref readonly BasicState entity)
            {
                base.OnRender(client, in entity);

                var color = new Color(entity.velocity.X, entity.velocity.Y, entity.velocity.Z, entity.timers[2]);

                float radius = entity.timers[0];
                float fade = entity.timers[1];

                client.LightManager.AddShadowmapped(new Engine.Common.LightManager2.LightConfig
                {
                    position = entity.position - new Vector3(0, Cube.CUBE_SCALE / 2, 0),
                    min = radius - fade,
                    max = radius,
                    color = color.ToVector4(),
                });
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    color = Color.White,
                    position = entity.position - new Vector3(0, Cube.CUBE_SCALE, 0),
                    shouldDraw = true,
                });
            }
        }

        private class RenderedLeviathan : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedLeviathan() : base("leviathan", GlobalState.Registry.EntityRegistry.Get<EntityLeviathan>().Id, new RendererDeferred.DrawMaterial("leviathan"))
            {
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                // TODO

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
            public RenderedHeart() : base("heart", GlobalState.Registry.EntityRegistry.Get<Heart>().Id, new RendererDeferred.DrawMaterial("heart"))
            {
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                float healthPercent = (float)entity.health / (float)Heart.MaxHealth;

                float interval = MathHelper.Lerp(0.25f, 2f, healthPercent);

                float t = (entity.aliveTime % interval) / interval;

                float s = MathF.Sin(MathF.PI * 2 * t) * 0.5f + 0.5f;

                float scale = MathHelper.Lerp(0.75f, 1.15f, s);

                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    position = entity.position,
                    sourceRect = new RectangleF(0, 0, 16, 21),
                    scale = new Vector2(scale),
                });
            }
        }

        private class RenderedPlayerBubble : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedPlayerBubble() : base("player_bubble", GlobalState.Registry.EntityRegistry.Get<PlayerBubble>().Id, new RendererDeferred.DrawMaterial("bubble"))
            {
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                float t0 = (entity.aliveTime % 1.75f) / 1.75f;
                float t1 = ((entity.aliveTime + 0.45f) % 2.05f) / 2.05f;
                float s0 = MathF.Sin(MathF.PI * 2 * t0) * 0.5f + 0.5f;
                float s1 = MathF.Sin(MathF.PI * 2 * t1) * 0.5f + 0.5f;

                float scaleX = MathHelper.Lerp(1f, 1.15f, s0);
                float scaleY = MathHelper.Lerp(0.95f, 1.15f, s1);

                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    scale = new Vector2(scaleX, scaleY),
                    sourceRect = entity.counters[0] == 0 ? new RectangleF(0, 0, 64, 64) : new RectangleF(0, 64, 64, 64),
                });
            }
        }

        private class RenderedSnake : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            private EntityHelper.DirectionalSourceRect directionalSourceRect = new()
            {
                front = new RectangleF(0, 16, 16, 16),
                sideRight = new RectangleF(0, 0, 32, 16),
                back = new RectangleF(32, 16, 16, 16)
            };

            public RenderedSnake() : base("snake", GlobalState.Registry.EntityRegistry.Get<Snake>().Id, new RendererDeferred.DrawMaterial("snake"))
            {
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                Vector2 scale = Vector2.One;

                var side = EntityHelper.GetEntityDirectionalSide(client.currInterpState.camera, Vector3.Transform(Vector3.Forward, entity.rotation), directionalSourceRect);
                if (side == EntityHelper.DirectionalSide.Left || side == EntityHelper.DirectionalSide.Right)
                {
                    scale = new Vector2(2, 1);
                }
                RectangleF sourceRect = EntityHelper.GetEntityDirectionalSourceRect(side, directionalSourceRect);

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
                    int frame = (int)((1 - (entity.timers[AIWalkerMelee.ATTACK_TIMER_INDEX] / 0.25f)) * NUM_FRAMES);

                    sourceRect.x = 32 * frame;
                }

                Color c = EntityRendererHelper.GetHurtColor(entity, AIWalkerMelee.INVULN_TIMER_INDEX);

                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    sourceRect = sourceRect,
                    scale = scale,
                    color = c,
                });
            }
        }

        private class RenderedSnakeFlying : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedSnakeFlying() : base("snake_flying", GlobalState.Registry.EntityRegistry.Get<SnakeFlying>().Id, new RendererDeferred.DrawMaterial("snake"))
            {
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
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

                Color c = EntityRendererHelper.GetHurtColor(entity, 2);

                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    sourceRect = sourceRectWings,
                    scale = new Vector2(2),
                    position = entity.position,

                    color = c,
                });

                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    sourceRect = sourceRectSnake,
                    scale = new Vector2(2),
                    position = entity.position,

                    color = c,
                });
            }
        }

        private class RenderedStoneBeetle : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            private EntityHelper.DirectionalSourceRect directionalSourceRect = new EntityHelper.DirectionalSourceRect()
            {
                front = new RectangleF(0, 0, 16, 16),
                sideRight = new RectangleF(0, 16, 16, 16),
                back = new RectangleF(0, 32, 16, 16)
            };

            public RenderedStoneBeetle() : base("stone_beetle", GlobalState.Registry.EntityRegistry.Get<StoneBeetle>().Id, new RendererDeferred.DrawMaterial("stone_beetle"))
            {
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                //DrawHelper3D.DrawLine(client.Renderer, client.currInterpState.camera, entity.position, entity.position + Vector3.Transform(Vector3.Forward, entity.rotation) * 3, Cube.CUBE_SCALE * 0.25f, StaticMaterials.FlatColor, RectangleF.Empty, Color.White);
                var camera = client.currInterpState.camera;
                RectangleF sourceRect = EntityHelper.GetEntityDirectionalSourceRect(camera, Vector3.Transform(Vector3.Forward, entity.rotation), directionalSourceRect);

                if (entity.state == (int)AIWalkerShooter.State.Normal)
                {
                    if (entity.velocity.Length() > Cube.CUBE_SCALE * 0.1f)
                    {
                        float animP = (entity.aliveTime % 0.75f) / 0.75f;

                        int frame = (int)(animP * 2f);

                        sourceRect.x += 16 * frame;
                    }
                }

                if (entity.counters[0] > 0)
                {
                    sourceRect = new RectangleF(0, 48, 16, 16);
                }

                Color c = EntityRendererHelper.GetHurtColor(entity, 0);

                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    position = entity.position,
                    sourceRect = sourceRect,
                    color = c,
                });
            }
        }

        private class RenderedTestNPC : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedTestNPC(int type) : base(GlobalState.Registry.EntityRegistry.Get(type).Identifier, type, new RendererDeferred.DrawMaterial(DrawHelper.WhitePixel))
            {
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                var drawPos = entity.position - new Vector3(0, Cube.CUBE_SCALE * 1.5f, 0);

                var color = Color.White;

                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    color = color,
                    position = drawPos,
                    scale = new Vector2(1, 2),
                    shouldDraw = true,
                });
            }
        }

        private class RenderedLightStressTest : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedLightStressTest() : base("light_stress_test", GlobalState.Registry.EntityRegistry.Get<LightStressTest>().Id, new RendererDeferred.DrawMaterial("glow_node"))
            {
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                // TODO
                //for (int i = 0; i < 64; i++)
                //{
                //    var worldTime = entity.aliveTime * LightStressTest.Speed;
                //    float t = ((worldTime + 0.03f * i) % 2f) / 2f;
                //    float zt = ((worldTime + 0.03f * i + 0.3f) % 2f) / 2f;

                //    float x = MathF.Cos(MathF.PI * 2 * t) * LightStressTest.RADIUS_XZ;
                //    float y = MathF.Sin(MathF.PI * 2 * t) * LightStressTest.RADIUS_Y;
                //    float z = -MathF.Sin(MathF.PI * 2 * zt) * LightStressTest.RADIUS_XZ;

                //    Vector3 lightPos = entity.position + new Vector3(x, y, z);


                //    cachedStats[i].position = lightPos;
                //}

                //return cachedStats;
            }
        }

        private class RenderedWorm : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedWorm() : base("worm", GlobalState.Registry.EntityRegistry.Get<Worm>().Id, new RendererDeferred.DrawMaterial("worm"))
            {
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                // TODO

                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    position = entity.position,
                    sourceRect = new RectangleF(0, 0, 16, 16),
                });


                //for (int i = 0; i < 8; i++)
                //{
                //    renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                //    {
                //        position = worm.trainPositions[i],
                //        sourceRect = new RectangleF(16, 0, 16, 16),
                //    });
                //}

                //return cachedStats;
            }
        }

        private class RenderedBonePillar : RendererOpaqueBillboardedEntity.RenderedEntity
        {
            public RenderedBonePillar() : base("bone_pillar", GlobalState.Registry.EntityRegistry.Get<BonePillar>().Id, new RendererDeferred.DrawMaterial("bone_pillar"))
            {
            }

            public override void GetDrawStats(ClientStates client, ref readonly BasicState entity, FastList<RendererOpaqueBillboardedEntity.RenderedEntityDrawStats> renderedEntityStats)
            {
                renderedEntityStats.Add(new RendererOpaqueBillboardedEntity.RenderedEntityDrawStats
                {
                    scale = new Vector2(1, 3),
                    position = entity.position,
                });
            }
        }

        private readonly RendererOpaqueBillboardedEntity renderer;

        public static void DoRegistration(RendererOpaqueBillboardedEntity renderer)
        {
            renderer.registry.Register(new RenderedImp());
            renderer.registry.Register(new RenderedSkeleton());
            renderer.registry.Register(new RenderedSkeleton2());
            renderer.registry.Register(new RenderedSkeletonBonePile());
            renderer.registry.Register(new RenderedSlime());
            renderer.registry.Register(new RenderedBigSlime());
            renderer.registry.Register(new RenderedCaveSlime());
            renderer.registry.Register(new RenderedGhost());
            renderer.registry.Register(new RenderedCultist());
            renderer.registry.Register(new RenderedGeneric("salamander", GlobalState.Registry.EntityRegistry.Get<CaveSalamander>().Id, new RendererDeferred.DrawMaterial("salamander"), sourceRect: new RectangleF(0, 0, 16, 16)));
            renderer.registry.Register(new RenderedDucken());
            renderer.registry.Register(new RenderedGhoul());
            renderer.registry.Register(new RenderedGlowNode());
            renderer.registry.Register(new RenderedLeviathan());
            renderer.registry.Register(new RenderedHeart());
            renderer.registry.Register(new RenderedPlayerBubble());
            renderer.registry.Register(new RenderedSnake());
            renderer.registry.Register(new RenderedSnakeFlying());
            renderer.registry.Register(new RenderedStoneBeetle());
            renderer.registry.Register(new RenderedTestNPC(GlobalState.Registry.EntityRegistry.Get<TestNPC>().Id));
            //renderer.registry.Register(new RenderedTestNPC(GlobalState.Registry.EntityRegistry.Get<Player>().Id));
            renderer.registry.Register(new RenderedLightStressTest());
            renderer.registry.Register(new RenderedWorm());
            renderer.registry.Register(new RenderedBonePillar());
        }
    }
}
