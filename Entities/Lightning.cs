using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Entities
{
    public class Lightning : Entity
    {
        private static (VertexBuffer VBO, IndexBuffer IBO) mesh;
        private static Color lightningColor = new Color(255, 253, 141);

        private Vector3 bottomPosition;

        private Vector3[] positions;

        private int light = -1;

        public Lightning(Vector3 position)
        {
            this.Position = position;
            AlwaysRender = true;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            bottomPosition = world.ChunkManager.InitializerView.GetFirstSolidDown(CubePosition.FromWorldSpace(Position)).Get().InWorldSpaceCenter();

            float SPLIT_DISTANCE = Cube.CUBE_SCALE * 4f;

            Vector3 direction = bottomPosition - Position;
            float distance = direction.Length();
            direction.Normalize();

            int numSplits = (int)(distance / SPLIT_DISTANCE);

            positions = new Vector3[numSplits + 1];

            for (int i = 0; i < numSplits; i++)
            {
                positions[i] = Position + direction * SPLIT_DISTANCE * (i + 1);
                positions[i] += new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 2f), 0, Main.random.NextFloat(-Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 2f));
            }

            positions[numSplits] = bottomPosition;

            world.LightManager.Add(bottomPosition, Cube.CUBE_SCALE * 4, Cube.CUBE_SCALE * 8, lightningColor.ToVector4(), false);
        }

        public override void OnUnload()
        {
            base.OnUnload();

            if (light != -1)
                world.LightManager.Remove(light);
        }

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

            if (mesh.VBO == null)
                mesh = MeshHelper.MakeEnemyQuad(device, 1, 1);

            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 prev;
                if (i == 0)
                    prev = Position;
                else prev = positions[i - 1];

                Vector3 current = positions[i];

                DrawHelper3D.DrawLine(prev, current, Cube.CUBE_SCALE / 4f, mesh, DrawHelper.WhitePixel, RectangleF.Empty, lightningColor);
            }

            //DrawHelper3D.DrawLine(Position, bottomPosition, Cube.CUBE_SCALE / 4f, mesh, DrawHelper.WhitePixel, RectangleF.Empty, Color.Yellow);
        }
    }
}
