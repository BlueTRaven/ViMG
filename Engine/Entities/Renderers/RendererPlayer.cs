using BepuPhysics.Constraints;
using BrUtility;
using Engine.Clients;
using Engine.Clients.Entities;
using Engine.Entities;
using Engine.Items;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D9;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Entities.Renderers;
using ViMG.Items;
using ViMG.Rendering;
using ViMG.VertexDeclarations;

namespace Engine.Entities.Renderers
{
    //Draws the local player
    public class RendererPlayer : EntityRenderer
    {
        private VerySimpleMesh mesh;
        private VerySimpleMesh lookAtMesh;

        public RendererPlayer(GraphicsDevice device) : base("player_local", device)
        {
            mesh = MeshHelper.MakeQuad(device, 1, 0.98f * 2f, Enums.Alignment.Center);
            lookAtMesh = MeshHelper.MakeCube(device, Vector3.Zero, new Vector3(Cube.CUBE_SCALE), VerySimpleMesh.Pass.Transparent);
        }

        private static int[]? types = null;
        public override int[] GetRenderedTypes()
        {
            if (types == null)
                types = [Main.Registry.EntityRegistry.Get<Player>().Id];
            return types;
        }

        public override void RenderClientEnt(GraphicsDevice device, double deltaTime, ClientStates client, int type)
        {
            if (client.LocalPlayer != null && client.ChunkManager.PhysicsInfo.Simulation.Bodies.BodyExists(client.LocalPlayer.Body)) {
                var pos = client.ChunkManager.PhysicsInfo.Simulation.Bodies[client.LocalPlayer.Body].Pose.Position;
                Main.Renderer.AddTransparentDraw(new RendererDeferred.TransparentDraw(0, new RendererDeferred.DrawMaterial(DrawHelper.WhitePixel),
                       lookAtMesh, Matrix.CreateTranslation(pos), new RectangleF(0, 1008 - 32, 16, 16)));
            }

            for (int i = 0; i < EntityManager.EntMax; i++)
            {
                var reference = client.Current().entities.GetReference(i);
                if (client.Current().entities.GetTypeById(reference.id) != type) continue;

                var entType = Main.Registry.EntityRegistry.Get(client.Current().entities.GetTypeById(reference.id));
                var entity = entType?.GetInterpolated(client, reference) ?? new();

                var extraState = entity.GetExtra<Player.PlayerExtraState>();
                Inventory? inventory = client.inventoryManager.Get(extraState.inventory);
                var highlightedItem = inventory?.Get(extraState.highlightIndex) ?? new();
                highlightedItem.item?.Client?.DrawInHand(device, highlightedItem, entity, -BasicState.Forward(ref entity));

                Vector3 ypr = EngineMathHelper.QuaternionToYawPitchRoll(entity.rotation.ToNumerics());

                Matrix worldMat = Matrix.CreateScale(Cube.CUBE_SCALE) *
                    Matrix.CreateRotationX(Math.Clamp(ypr.Y, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                    Matrix.CreateRotationY(ypr.X) *
                    Matrix.CreateTranslation(entity.position);

                Color color = Color.White;
                Main.Renderer.AddOpaqueDraw(new RendererDeferred.GBufferDraw(new RendererDeferred.DrawMaterial(DrawHelper.WhitePixel),
                    mesh, worldMat, null, color.ToVector3()));
                Main.Renderer.AddOpaqueDraw(new RendererDeferred.GBufferDraw(new RendererDeferred.DrawMaterial(DrawHelper.WhitePixel),
                    mesh, worldMat, null, color.ToVector3()));

                var fwd = BasicState.Forward(ref entity);
                var lookAtResult = CubeView.Raycast(entity.position, entity.position - fwd * Player.INTERACT_DISTANCE, CubeView.RaycastCallbackTouchable, client.ChunkManager.CubeView);

                float s = MathF.Sin(MathF.PI * 2f * ((float)Main.Time % 2f)) * 0.5f + 0.5f;
                Color lookAtColor = Color.Lerp(Color.White, Color.Black, s);

                if (lookAtResult.hasHit)
                {
                    //bool expandedMine = entity.state == (int)Player.State.Normal && client.CurrMovement.MoveDown.Pressed();
                    //if (expandedMine &&
                    //    highlightedItem.valid && highlightedItem.item is IHasAreaEffect pickStats)
                    //{
                    //    CubePosition[] positions = pickStats.GetAffectedPositions(client.ChunkManager.CubeView, highlightedItem, entity.position, extraState.lookAtPos.InWorldSpace(), lookAtResult.normal, out _);

                    //    Span<ushort> ids = stackalloc ushort[positions.Length];
                    //    client.ChunkManager.CubeView.GetIds(positions.AsSpan(), ids);

                    //    for (int j = 0; j < positions.Length; j++)
                    //    {
                    //        if (pickStats.CanPredictAir() || Main.Registry.CubeRegistry.GetOrDefault(ids[j], Main.Registry.CubeRegistry.Air).Touchable)
                    //        {
                    //            Main.Renderer.AddTransparentDraw(new RendererDeferred.TransparentDraw((int)lookAtResult.end.Length(), StaticMaterials.Cubes,
                    //                lookAtMesh,
                    //                Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 2f)) *
                    //                Matrix.CreateScale(1.126f) *
                    //                Matrix.CreateTranslation(new Vector3(Cube.CUBE_SCALE / 2f)) *
                    //                Matrix.CreateTranslation(positions[j].InWorldSpace()),
                    //                new RectangleF(0, 1008, 16, 16), color));
                    //        }
                    //    }
                    //}
                    //else
                    {
                        Main.Renderer.AddTransparentDraw(new RendererDeferred.TransparentDraw((int)lookAtResult.hit.Length(), StaticMaterials.Cubes,
                            lookAtMesh,
                            Matrix.CreateTranslation(CubePosition.RoundToCubeSpace(lookAtResult.hit)),
                            new RectangleF(0, 1008, 16, 16), lookAtColor));
                    }
                }

                if (lookAtResult.hasHit && client.ChunkManager.CubeView.GetCube(CubePosition.FromWorldSpace(lookAtResult.hit))
                    .GetOrDefault(Main.Registry.CubeRegistry.Air).CanRightClick(CubePosition.FromWorldSpace(lookAtResult.hit)))
                {
                    //? crosshair
                    Main.CrosshairSourceRect = new RectangleF(16, 0, 16, 16);
                }
                else Main.CrosshairSourceRect = new RectangleF(0, 0, 16, 16);

                //if (Main.gameStateManager.TheIsland.GetWorld() != null)
                //{
                //    var srvPlayer = Main.gameStateManager.TheIsland.GetWorld().EntityManager.GetByRef(reference) as Player;

                //    var fwd2 = (srvPlayer as IRotatable).Forward;
                //    var lookAtResult2 = CubeView.Raycast(srvPlayer.Position, srvPlayer.Position - fwd2 * Player.INTERACT_DISTANCE, CubeView.RaycastCallbackSolid, Main.gameStateManager.TheIsland.GetWorld().ChunkManager.CubeView);

                //    if (lookAtResult2.hasHit)
                //    {
                //        Main.Renderer.AddTransparentDraw(new RendererDeferred.TransparentDraw((int)lookAtResult.hit.Length(), StaticMaterials.Cubes,
                //               lookAtMesh,
                //               Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 2f)) *
                //               Matrix.CreateScale(1.126f) *
                //               Matrix.CreateTranslation(new Vector3(Cube.CUBE_SCALE / 2f)) *
                //               Matrix.CreateTranslation(CubePosition.RoundToCubeSpace(lookAtResult2.hit)),
                //               new RectangleF(0, 1008, 16, 16), Color.Red));
                //    }
                //}
            }
        }

        public override void RenderUI(GraphicsDevice device, SpriteBatch batch, double deltaTime, ClientStates client, int entityType)
        {
            base.RenderUI(device, batch, deltaTime, client, entityType);

            var curr = client.Current();
            var prev = client.Previous(1);
            float interpTime = (float)double.Lerp(prev.time, curr.time, Main.TimeC);
            var entity = Main.Registry.EntityRegistry.Get<Player>().GetInterpolated(client, curr.entities.GetLocalPlayerRef());
            var player = entity.GetExtra<Player.PlayerExtraState>();

            var inventory = client.inventoryManager.Get(player.inventory);
            var heldInventory = client.inventoryManager.Get(player.heldInventory);
            var item = inventory.Get(curr.highlightIndex);
            var heldItem = heldInventory.Get(0);
            item.item?.Client.Hold(client, entity, inventory, curr.highlightIndex);
            heldItem.item?.Client.Hold(client, entity, inventory, curr.highlightIndex);

            if (entity.state == (int)Player.State.Dead)
            {
                //interpTime - player.deadTime < Player.DEAD_TIME
                float t = (interpTime - player.deadTime) / Player.DEAD_TIME;
                t = float.Clamp(t, 0, 1);

                batch.DrawRectangle(new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y), Color.Black * t);
            }

            if (entity.aliveTime < 0.5f)
            {
                float t = 1 - (entity.aliveTime / 0.5f);

                batch.DrawRectangle(new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y), Color.Black * t);
            }

            if (interpTime - player.damageTime < Player.DAMAGE_ANIM_TIME)
            {
                float t = 1 - ((interpTime - player.damageTime) / Player.DAMAGE_ANIM_TIME);

                batch.DrawRectangle(new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y), Color.DarkRed * t);
            }

            //if (headUnderWater)
            //{
            //    batch.DrawRectangle(new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y), Color.Blue * 0.5f);
            //}
        }
    }
}
