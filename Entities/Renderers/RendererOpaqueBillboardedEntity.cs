using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Items;
using ViMG.Rendering;

namespace ViMG.Entities.Renderers
{
    public class RendererOpaqueBillboardedEntity : EntityRenderer
    {
        private class TypeStats
        {
            public RendererDeferred.DrawMaterial Material;
            public FastList<RendererDeferred.InstancedDraw> Draws;  //we cache a list here so we don't have to always allocate during a frame.
            public StructuredBuffer SBO;

            public Vector2 Scale;

            public TypeStats(RendererDeferred.DrawMaterial material, Vector2? scale = null)
            {
                this.Material = material;
                this.Scale = scale ?? Vector2.One;
                Draws = new FastList<RendererDeferred.InstancedDraw>();
            }
        }

        private TypeStats[] typeStats;
        private Type[] renderedTypes = new Type[]
        {
            typeof(Imp),
            typeof(Skeleton),
            typeof(Slime),
            typeof(SlimeBig),
            typeof(CaveSlime),
        };

        public (VertexBuffer VBO, IndexBuffer IBO) mesh;

        public RendererOpaqueBillboardedEntity(GraphicsDevice device) : base("generic_billboard", device)
        {
            typeStats = new TypeStats[]
            {
                new TypeStats(new RendererDeferred.DrawMaterial("imp")),
                new TypeStats(new RendererDeferred.DrawMaterial("skeleton"), new Vector2(1, 2)),
                new TypeStats(new RendererDeferred.DrawMaterial("slime")),
                new TypeStats(new RendererDeferred.DrawMaterial("slime"), new Vector2(2)),
                new TypeStats(new RendererDeferred.DrawMaterial("slime")),
            };

            mesh = MeshHelper.MakeEnemyQuad(device, 1, 1);
        }

        public override Type[] GetRenderedTypes()
        {
            return renderedTypes;
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex)
        {
            Type type = renderedTypes[renderedTypeIndex];
            TypeStats stats = typeStats[renderedTypeIndex];

            var entities = entityManager.GetAll(type);

            stats.Draws.Clear();

            Matrix billboard = Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
                    Matrix.CreateRotationY(-Main.camera.Rotation.Y);

            RendererDeferred.InstancedDraw baseDraw = new RendererDeferred.InstancedDraw()
            {
                SourceRect = new RendererDeferred.DrawSourceRectParameters(new RectangleF(0, 16, 16, 16)),
                TintColor = Color.White.ToVector3(),
            };

            foreach (Entity entity in entities)
            {
                Matrix mat = Matrix.CreateScale(Cube.CUBE_SCALE) *
                    billboard *
                    Matrix.CreateTranslation(entity.Position);
                Matrix.Transpose(ref mat, out mat);

                RendererDeferred.InstancedDraw draw = baseDraw with
                {
                    World = mat,
                    WorldNormal = Matrix.Transpose(Matrix.Invert(mat)),
                };

                if (type == typeof(SlimeBig))
                {
                    SlimeBig slime = entity as SlimeBig;

                    draw.SourceRect = new RendererDeferred.DrawSourceRectParameters(SlimeBig.GetSourceRect(slime));
                }

                stats.Draws.Add(draw);
            }

            if (stats.SBO == null || stats.SBO.ElementCount < stats.Draws.Length)
            {
                if (stats.SBO != null)
                    stats.SBO.Dispose();

                stats.SBO = new StructuredBuffer(device, typeof(RendererDeferred.InstancedDraw), stats.Draws.Buffer.Length, BufferUsage.WriteOnly, ShaderAccess.Read);
            }

            stats.SBO.SetData(stats.Draws.Buffer);

            Main.Renderer.DrawsPassGBufferInstanced.Add(new RendererDeferred.InstancedGBufferDraw(
                stats.Material, mesh.VBO, mesh.IBO, stats.SBO, 0, stats.Draws.Length));
        }
    }
}
