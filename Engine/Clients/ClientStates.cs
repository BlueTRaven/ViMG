using BepuPhysics;
using BepuPhysics.Constraints;
using BrAssetsManager;
using Engine.Clients.WorldLogics;
using Engine.Common;
using Engine.Common.Entities;
using Engine.IMGUIImpl;
using Engine.Items;
using Engine.Networking;
using Engine.Networking.Messages;
using Engine.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;
using ViMG.IMGUIImpl;
using ViMG.Physics;
using ViMG.Rendering;
using ViMG.UIs;

namespace Engine.Clients
{
    public class ClientStates : IDisposable
    {
        [ConsoleCommandVar("r_render_client_ents")]
        public static bool RenderClientEnts = true;
        [ConsoleCommandVar("r_render_projectiles")]
        public static bool RenderProjectiles = true;
        [ConsoleCommandVar("r_render_lights")]
        public static bool RenderLights = true;
        [ConsoleCommandVar("r_render_world")]
        public static bool RenderWorld = true;

        [ConsoleCommandVar("rsv_render_debug_physics")]
        public static bool RenderDebugPhysics = false;

        private GraphicsDevice device;
        public ClientWorld[] states;
        public ClientWorld currInterpState;
        public ClientWorld prevInterpState;
        public ClientInventoryManager inventoryManager;
        public ClientChunkManager ChunkManager;
        public ClientWorldLogic WorldLogic;
        private WorldRenderer worldRenderer;
        //public LightManager LightManager;
        public LightManager2 LightManager;
        private LightsRenderer lightRenderer;
        private PhysicsInfo physicsInfo;

        public RendererDeferred Renderer;

        private Engine.Rendering.BepuDebugRendering.Renderer bepuDebugRenderer;

        public ClientLocalPlayer? LocalPlayer = null;

        public int LocalPlayerIndex => Main.gameStateManager.TheIsland.netManagerClient?.whoAmI ?? -1;

        private int head = 0;
        private int frame = 0;

        public DateTime Started;
        public DateTime LastFramePrecise;
        public double LastFrameTime;
        public double Variance;
        public double CurrentTime;
        public double RenderTime;
        public double LastFrameRenderTime;
        private bool disposedValue;

        //public Camera InterpCamera = null;

        public double TimeC => 1 - (((LastFrameRenderTime + World.SyncTime) - RenderTime) / World.SyncTime);

        public ClientStates(GraphicsDevice device)
        {
            Renderer = new RendererDeferred(device);

            Started = DateTime.Now;

            this.device = device;

            physicsInfo = new PhysicsInfo();

            ChunkManager = new ClientChunkManager(device, physicsInfo);

            states = new ClientWorld[ViMG.Entities.EntityManager.EntPrevSrv];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = new ClientWorld();
            }
            currInterpState = new ClientWorld();
            prevInterpState = new ClientWorld();

            //InterpCamera = new CameraPerspective(states[0].camera.Position, states[0].camera.RotationEuler, states[0].camera.Scale, Main.FOV_DEGREES, Main.NEAR, Main.FAR);

            inventoryManager = new ClientInventoryManager();

            // TODO how to support multiple layers?
            WorldLogic = Activator.CreateInstance(GlobalState.Registry.WorldLogicRegistry.clientLogics[0], device) as ClientWorldLogic;
            worldRenderer = new WorldRenderer(device);

            //LightManager = new LightManager(device);
            LightManager = new LightManager2();
            lightRenderer = new LightsRenderer(device);

            bepuDebugRenderer = new Rendering.BepuDebugRendering.Renderer(device, null);
        }

        public void NewFrame(int sequence, double time, DateTime timePrecise)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

            frame += 1;

            double expectedArrivalTime = LastFrameTime + World.SyncTime;

            LastFrameTime = time;
            CurrentTime = time;

            LastFramePrecise = timePrecise;

            LastFrameRenderTime = RenderTime;

            Variance = time - expectedArrivalTime;

            //Console.WriteLine("New frame {0} time {1:.0000}s expected {2:.0000}s variance {3:.0000}s {4}", frame, time, expectedArrivalTime, double.Abs(Variance), Variance > 0 ? "early" : "late");

            ClientWorld prev = Current();
            head = (head + 1) % ViMG.Entities.EntityManager.EntPrevSrv;
            Current().NewFrame(prev, time);
            prevInterpState.NewFrame(currInterpState, currInterpState.time);
            currInterpState.NewFrame(prev, time);

            SyncInventoryUpdate.Instance.Apply(inventoryManager);
        }

        public ClientWorld Current()
        {
            return states[head];
        }

        public ClientWorld Previous(int prev)
        {
            int which = head - prev;
            which = ((which % ViMG.Entities.EntityManager.EntPrevSrv) + ViMG.Entities.EntityManager.EntPrevSrv) % ViMG.Entities.EntityManager.EntPrevSrv;
            return states[which];
        }

        public void UpdatePlayer(double deltaTime)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

            Renderer.Update(deltaTime);

            ChunkManager.PhysicsInfo.Simulation.Timestep((float)deltaTime);
            ChunkManager.CubeProgressTracker.Update(ChunkManager.CubeView, deltaTime);

            WorldLogic.UpdateSimulation(deltaTime, this);

            var current = Current();
            var previous = Previous(1);

            current.projectiles.Update(ChunkManager.CubeView, deltaTime);

            LocalPlayer?.Update(this, deltaTime);
        }

        public void Render(GraphicsDevice device, SpriteBatch batch, double deltaTime)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

            Renderer.FrameStart();

            if (Main.gameStateManager.GetCurrentGameState().GetCurrentMenu() is not MenuPause)
                RenderTime += deltaTime;

            {
                var prevCamera = prevInterpState.camera;
                var currCamera = Current().camera;
                currInterpState.camera.Position = Vector3.Lerp(prevCamera.Position, currCamera.Position, (float)TimeC);
                //currInterpState.camera.Rotation = Quaternion.Lerp(prevCamera.Rotation, currCamera.Rotation, (float)TimeC);
                currInterpState.camera.Scale = Vector3.Lerp(prevCamera.Scale, currCamera.Scale, (float)TimeC);

                for (int i = 0; i < EntityManager.EntMax; i++)
                {
                    var reference = currInterpState.entities.GetReference(i);
                    if (reference.id == -1) continue;

                    ref var ent = ref currInterpState.entities.GetByRefPtr(ref reference);

                    int typeId = currInterpState.entities.GetTypeById(reference.id);

                    ent = GlobalState.Registry.EntityRegistry.Get(typeId)?.GetInterpolated(this, reference) ?? new();
                }

                currInterpState.time = (float)double.Lerp(prevInterpState.time, Current().time, TimeC);
            }

            if (RenderLights)
            {
                lightRenderer.UpdateDatas(LightManager, Renderer.EffectLightAccumPointLight);
                lightRenderer.Draw(device, Renderer, LightManager);
                lightRenderer.DrawShadowmap(device, Renderer, ChunkManager, LightManager);
            }

            LightManager.Reset();
            //LightManager.UpdateDatas(Main.Renderer.EffectLightAccumPointLight);
            //LightManager.DrawShadowmap(device, ChunkManager);
            //LightManager.Draw(device);

            if (RenderWorld)
            {
                WorldLogic.Render(device, this);

                worldRenderer.Render(this);
            }

            if (RenderClientEnts)
            {
                var iter = GlobalState.Registry.RendererRegistry.GetIterable();
                foreach (var a in iter)
                {
                    int[] renderedTypes = a.GetRenderedTypes();
                    foreach (int t in renderedTypes)
                    {
                        a.RenderClientEnt(device, deltaTime, this, t);
                    }
                }
            }

            if (RenderProjectiles)
                ClientProjectileManager.Render(device, this);

            if (RenderDebugPhysics && Main.gameStateManager.TheIsland.GetWorld() != null)
            {
                bepuDebugRenderer.Shapes.ClearInstances();
                bepuDebugRenderer.Shapes.AddInstances(Main.gameStateManager.TheIsland.GetWorld().PhysicsInfo.Simulation);
                bepuDebugRenderer.Render(device, currInterpState.camera);
            }

            if (Main.gameStateManager.TheIsland.GetWorld() != null)
            {
                Main.gameStateManager.TheIsland.GetWorld().HitboxManager.DrawDebug(device, Renderer, currInterpState.camera);
            }

            Renderer.Draw(batch, currInterpState.camera);

            if (Renderer.GetOutput() != null)
            {
                device.SetRenderTarget(null);

                batch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, null);

                batch.Draw(Renderer.GetOutput().Value.RenderTarget as RenderTarget2D, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

                batch.End();
            }
        }

        public void RenderUI(GraphicsDevice device, SpriteBatch batch, double deltaTime)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

            var iter = GlobalState.Registry.RendererRegistry.GetIterable();
            foreach (var a in iter)
            {
                int[] renderedTypes = a.GetRenderedTypes();
                foreach (int t in renderedTypes)
                {
                    a.RenderUI(device, batch, deltaTime, this, t);
                }
            }

            if (LocalPlayer != null)
                batch.Draw(GlobalState.AssetsManager.GetAsset<Texture2D>("crosshair"), new Vector2(Options.CurrentWindowResolution.X / 2 - 8,
                    Options.CurrentWindowResolution.Y / 2 - 8), Main.CrosshairSourceRect.ToRectangle(), Color.White);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    ChunkManager.ChunkMesher.Dispose();

                    ChunkManager.PhysicsInfo.Simulation.Dispose();
                    ChunkManager.PhysicsInfo.Properties.Dispose();
                    ChunkManager.PhysicsInfo.GlobalBufferPool.Clear();

                    bepuDebugRenderer.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ClientStates()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
