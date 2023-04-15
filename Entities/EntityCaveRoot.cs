using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;
using ViMG.VertexDeclarations;
using ViMG.Rendering;

namespace ViMG.Entities
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
    [EntityMeta(0)]
    public class EntityCaveRoot : Entity, ICubeTracker
    {
        private record struct Save
        {
            public CubePosition trackedPosition;
            public float creationTime;
            public float grownTime;     //the time after which this plant will be considered fully grown.
        }

        private static (VertexBuffer VBO, IndexBuffer IBO) mesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("cubes_textures");

        private Save save;
        public CubePosition TrackedPosition => save.trackedPosition;

        public EntityCaveRoot()
        {
        }

        public EntityCaveRoot(CubePosition position)
        {
            save = new Save();
            save.trackedPosition = position;
            this.Position = position.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2f, 0, Cube.CUBE_SCALE / 2f);
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            save.creationTime = world.GetTime();
            save.grownTime = GetGrownTime(world);
        }

        private static float GetGrownTime(World world)
        {
            return world.GetTime() + Main.random.NextFloat(6, 8); //TODO actual growth time
        }

        public bool OnInteract(Player player)
        {
            if (player.world.GetTime() > save.grownTime)
            {
                save = save with
                {
                    creationTime = player.world.GetTime(),
                    grownTime = GetGrownTime(world)
                };

                world.EntityManager.Add(new EntityItem(player.Position, Vector3.Zero, new Items.ItemInstance(Main.Registry.ItemRegistry.Get("food_root1"), 1, 1)));
                return true;
            }
            return false;
        }

        public void TrackingCubeUpdated(World world, ChunkManager cm, ushort updatedId)
        {
            world.EntityManager.Remove(this);
        }

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

            if (mesh.VBO == null)
            {
                List<VertexCube> vertices = new List<VertexCube>();
                List<int> indices = new List<int>();
                DrawHelper3D.MakeXMeshRaw(vertices, indices, Vector3.Zero, Vector3.One, new RectangleF(0, 0, 1, 1));

                mesh = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices);
            }

            RectangleF sourceRect = new RectangleF(0, 176, 16, 16);

            if (world.GetTime() <= save.grownTime)
            {
                float growthP = (world.GetTime() - save.creationTime) / (save.grownTime - save.creationTime);

                const int stages = 3;

                int currentStage = (int)((float)stages * growthP);

                sourceRect.x = currentStage * 16;
            }
            else sourceRect.x = 2 * 16;

            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(
                material, mesh.VBO, mesh.IBO, Matrix.CreateTranslation(Position), sourceRect));
        }

        public unsafe override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

            SaveHelper.SaveFloat32(saveBytes, save.creationTime);
            SaveHelper.SaveFloat32(saveBytes, save.grownTime);

            SaveHelper.SaveCubePosition(saveBytes, save.trackedPosition);
        }

        public unsafe override void OnLoad(byte[] loadBytes, in int version)
        {
            base.OnLoad(loadBytes, version);

            int offset = 0;
            save.creationTime = SaveHelper.LoadFloat32(loadBytes, ref offset);
            save.grownTime = SaveHelper.LoadFloat32(loadBytes, ref offset);
            save.trackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref offset);

            this.Position = save.trackedPosition.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2f, 0, Cube.CUBE_SCALE / 2f);
        }
    }
}
