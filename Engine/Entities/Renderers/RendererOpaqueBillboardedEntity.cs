using BepuPhysics.Constraints;
using BrUtility;
using Engine.Clients;
using Engine.Networking;
using Engine.Networking.Messages;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using SharpDX.Direct3D9;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.IMGUIImpl;
using ViMG.Items;
using ViMG.Rendering;

namespace ViMG.Entities.Renderers
{
    public class RendererOpaqueBillboardedEntity : EntityRenderer
    {
        public record struct RenderedEntityDrawStats
        {
            public bool shouldDraw = true;

            public RectangleF? sourceRect;
            public Vector3? position; //if null, just uses entity position!
            public Vector2? scale;

            public Color? color = Color.White;

            public RenderedEntityDrawStats() { }
        }

        private int[] rendererMapping = [];
        public abstract class RenderedEntity : IRegisterable
        {
            public string Identifier { get; set; }

            public RendererDeferred.DrawMaterial Material;
            public FastList<RendererDeferred.InstancedDraw> Draws;  //we cache a list here so we don't have to always allocate during a frame.
            public StructuredBuffer SBO;
            public int EntityTypeId;

            public RenderedEntity(string identifier, int entityTypeId, RendererDeferred.DrawMaterial material)
            {
                this.Identifier = identifier;
                this.EntityTypeId = entityTypeId;
                this.Material = material;
                Draws = new FastList<RendererDeferred.InstancedDraw>();
            }

            public virtual void OnRender(ClientStates client, ref readonly BasicState entity) { }

            public virtual RenderedEntityDrawStats[] GetDrawStats(ref readonly BasicState entity)
            {
                return Array.Empty<RenderedEntityDrawStats>();
            }
        }

        public ObjRegistry<RenderedEntity> registry;

        public VerySimpleMesh mesh;

        public RendererOpaqueBillboardedEntity(GraphicsDevice device) : base("generic_billboard", device)
        { 
            mesh = MeshHelper.MakeQuad(device, 1, 1, Enums.Alignment.Bottom);

            registry = new ObjRegistry<RenderedEntity>();
        }

        private int[]? renderedTypesCache = null;
        public override int[] GetRenderedTypes()
        {
            if (renderedTypesCache == null)
            {
                renderedTypesCache = new int[registry.Count];
                var riter = registry.GetIterable();
                int max = int.MinValue;
                for (int i = 0; i < riter.Length; i++)
                {
                    var stats = riter[i];
                    max = int.Max(stats.EntityTypeId, max);
                }
                rendererMapping = new int[max + 1];

                for (int i = 0; i < riter.Length; i++)
                {
                    var stats = riter[i];
                    renderedTypesCache[i] = stats.EntityTypeId;
                    rendererMapping[stats.EntityTypeId] = i + 1;
                }
            }
            return renderedTypesCache;
        }

        public override void RenderClientEnt(GraphicsDevice device, double deltaTime, ClientStates client, int type)
        {
            // Need to convert global entity type registry (type) to local renderer registry. How do we do this without expensive dict lookup? Sparse array?
            var renderer = registry.Get(rendererMapping[type]);
            if (renderer == null) return;
            renderer.Draws.Clear();

            var prev = client.Previous(1);
            var current = client.Current();
            var camera = client.InterpCamera;
            Matrix billboard = Matrix.CreateRotationX(Math.Clamp(camera.RotationEuler.Y, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                    Matrix.CreateRotationY(camera.RotationEuler.X);

            RendererDeferred.InstancedDraw baseDraw = new RendererDeferred.InstancedDraw()
            {
                SourceRect = new RendererDeferred.DrawSourceRectParameters(new RectangleF(0, 16, 16, 16)),
                TintColor = Color.White.ToVector3(),
            };

            for (int i = 0; i < current.entities.MaxEnts; i++)
            {
                var reference = current.entities.GetReference(i);
                // TODO get rid of str compare
                if (current.entities.GetTypeById(reference.id) != type) continue;

                var entInterp = Main.Registry.EntityRegistry.Get(type).GetInterpolated(client, reference);
                //var entCurr = client.Current().entities.GetById(reference.id);
                //var entPrev = client.Previous(1).entities.GetById(reference.id);

                renderer.OnRender(client, entInterp);
                var drawStats = renderer.GetDrawStats(entInterp);

                foreach (RenderedEntityDrawStats drawStat in drawStats)
                {
                    Vector2 scale = drawStat.scale ?? new Vector2(1);
                    Color color = drawStat.color ?? Color.White;
                    Vector3 position = drawStat.position ?? entInterp.position;

                    RendererDeferred.DrawSourceRectParameters sourceRect;
                    if (drawStat.sourceRect.HasValue)
                    {
                        sourceRect = new RendererDeferred.DrawSourceRectParameters(drawStat.sourceRect.Value);
                    }
                    else sourceRect = new RendererDeferred.DrawSourceRectParameters();

                    Matrix mat = Matrix.CreateScale(Cube.CUBE_SCALE) *
                        Matrix.CreateScale(scale.X, scale.Y, 1) *
                        billboard *
                        Matrix.CreateTranslation(position);
                    Matrix.Transpose(ref mat, out mat);

                    if (color.A == 255)
                    {
                        RendererDeferred.InstancedDraw draw = baseDraw with
                        {
                            World = mat,
                            WorldNormal = Matrix.Transpose(Matrix.Invert(mat)),
                            SourceRect = sourceRect,
                            TintColor = color.ToVector3(),
                        };

                        renderer.Draws.Add(draw);
                    }
                    else
                    {
                        float distance = (camera.Position - position).Length();

                        RendererDeferred.TransparentDraw draw = new RendererDeferred.TransparentDraw
                        {
                            Material = renderer.Material,
                            SourceRect = sourceRect,
                            TintColor = color.ToVector4(),
                            Mesh = mesh,
                            Transform = mat,
                            SortValue = distance,
                        };

                        Main.Renderer.AddTransparentDraw(draw);
                    }
                }
            }

            if (renderer.SBO == null || renderer.SBO.ElementCount < renderer.Draws.Length)
            {
                if (renderer.SBO != null)
                    renderer.SBO.Dispose();

                renderer.SBO = new StructuredBuffer(device, typeof(RendererDeferred.InstancedDraw), renderer.Draws.Buffer.Length, BufferUsage.WriteOnly, ShaderAccess.Read);
            }

            renderer.SBO.SetData(renderer.Draws.Buffer);

            Main.Renderer.DrawsPassGBufferInstanced.Add(new RendererDeferred.InstancedGBufferDraw(
                renderer.Material, mesh, renderer.SBO, 0, renderer.Draws.Length));
        }
    }
}
