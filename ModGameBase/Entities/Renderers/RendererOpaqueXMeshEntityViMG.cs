using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Entities.Renderers
{
    public static class RendererOpaqueXMeshEntityViMG
    {
        private class RenderedEntityAncientAltar : RendererOpaqueXMeshEntity.RenderedEntity
        {
            public RenderedEntityAncientAltar() : base("ancient_altar", Main.Registry.EntityRegistry.Get<AncientAltar>().Id, new RendererDeferred.DrawMaterial("cubes_textures"))
            {
            }

            private static RendererOpaqueXMeshEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueXMeshEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueXMeshEntity.RenderedEntityDrawStats[] GetDrawStats(Entity entity)
            {
                cachedStats[0] = new RendererOpaqueXMeshEntity.RenderedEntityDrawStats
                {
                    position = entity.Position + new Vector3(0, Cube.CUBE_SCALE, 0),
                    sourceRect = new RectangleF(112, 16, 16, 16),
                };

                return cachedStats;
            }
        }

        private class RenderedEntityCaveRoot : RendererOpaqueXMeshEntity.RenderedEntity
        {
            public RenderedEntityCaveRoot() : base("cave_root", Main.Registry.EntityRegistry.Get<EntityCaveRoot>().Id, new RendererDeferred.DrawMaterial("cubes_textures"))
            {
            }

            private static RendererOpaqueXMeshEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueXMeshEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueXMeshEntity.RenderedEntityDrawStats[] GetDrawStats(Entity entity)
            {
                EntityCaveRoot caveRoot = entity as EntityCaveRoot;

                RectangleF sourceRect = new RectangleF(0, 176, 16, 16);

                if (entity.world.GetTime() <= caveRoot.save.grownTime)
                {
                    float growthP = (entity.world.GetTime() - caveRoot.save.creationTime) / (caveRoot.save.grownTime - caveRoot.save.creationTime);

                    const int stages = 3;

                    int currentStage = (int)((float)stages * growthP);

                    sourceRect.x = currentStage * 16;
                }
                else sourceRect.x = 2 * 16;

                cachedStats[0] = new RendererOpaqueXMeshEntity.RenderedEntityDrawStats
                {
                    sourceRect = sourceRect,
                };

                return cachedStats;
            }
        }

        private class RenderedEntitySapling : RendererOpaqueXMeshEntity.RenderedEntity
        {
            public RenderedEntitySapling() : base("sapling", Main.Registry.EntityRegistry.Get<Sapling>().Id, new RendererDeferred.DrawMaterial("cubes_textures"))
            {
            }

            private static RendererOpaqueXMeshEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueXMeshEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueXMeshEntity.RenderedEntityDrawStats[] GetDrawStats(Entity entity)
            {
                cachedStats[0] = new RendererOpaqueXMeshEntity.RenderedEntityDrawStats
                {
                    position = entity.Position + new Vector3(Cube.CUBE_SCALE / 2f, 0, Cube.CUBE_SCALE / 2f),
                    sourceRect = (entity as Sapling).GetSourceRect(),
                };
                return cachedStats;
            }
        }

        private class RenderedEntityCaveCompass : RendererOpaqueXMeshEntity.RenderedEntity
        {
            public RenderedEntityCaveCompass() : base("cave_compass", Main.Registry.EntityRegistry.Get<EntityCaveCompass>().Id, new RendererDeferred.DrawMaterial("cubes_textures"))
            {
            }

            private static RendererOpaqueXMeshEntity.RenderedEntityDrawStats[] cachedStats = new RendererOpaqueXMeshEntity.RenderedEntityDrawStats[1];
            public override RendererOpaqueXMeshEntity.RenderedEntityDrawStats[] GetDrawStats(Entity entity)
            {
                cachedStats[0] = new RendererOpaqueXMeshEntity.RenderedEntityDrawStats
                {
                    matrix = (entity as EntityCaveCompass).GetMatrix(),
                    sourceRect = new RectangleF(16, 16, 16, 16),
                    shouldDraw = true,
                    color = Color.White,
                };
                return cachedStats;
            }
        }

        public static void DoRegistration(RendererOpaqueXMeshEntity renderer)
        {
            renderer.registry.Register(new RenderedEntityAncientAltar());
            renderer.registry.Register(new RenderedEntityCaveRoot());
            renderer.registry.Register(new RenderedEntitySapling());
        }
    }
}
