using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Items;

namespace ViMG.Cubes
{
    public class CubeFibrousPlant : Cube
    {
        public CubeFibrousPlant() : base("fibrous_plant", new RectangleF(48, 80, 16, 16), Color.White, 1)
        {
            Transparency = TransparencyValue.Transparent;
			Collision = CollisionValue.None;
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }

        public override void MakeVerts(Vector3 pos, Vector3 min, Vector3 max, CubeVisualInstance visual, Cube cube, List<VertexPositionColorTextureNormal> vertices, List<int> indices)
        {
			//Can just toss old values out the window, don't care...
			Vector3 xMin = -new Vector3(Cube.CUBE_SCALE / 2, 0, -Cube.CUBE_SCALE / 2);
			Vector3 xMax = new Vector3(Cube.CUBE_SCALE / 2, Cube.CUBE_SCALE, Cube.CUBE_SCALE / 2);

			const int textureWidth = 1024;
			const int textureHeight = 1024;

			const float cubeSideWidth = 1f / textureWidth;
			const float cubeSideHeight = 1f / textureHeight;

			//face doesn't matter, any works
			RectangleF sourceRect = cube.GetSourceRect(MeshHelper.CubeFace.RIGHT);

			Vector2 uvNear = new Vector2(sourceRect.x * cubeSideWidth, sourceRect.y * cubeSideHeight);
			Vector2 uvFar = new Vector2((sourceRect.x + sourceRect.width) * cubeSideWidth, (sourceRect.y + sourceRect.height) * cubeSideHeight);

			Vector3 a = new Vector3(xMax.X, xMin.Y, xMax.Z);
			Vector3 b = new Vector3(xMin.X, xMin.Y, xMax.Z);
			Vector3 c = new Vector3(xMin.X, xMax.Y, xMax.Z);
			Vector3 d = new Vector3(xMax.X, xMax.Y, xMax.Z);

			a = Vector3.Transform(a, Matrix.CreateRotationY(MathHelper.ToRadians(45)));
			b = Vector3.Transform(b, Matrix.CreateRotationY(MathHelper.ToRadians(45)));
			c = Vector3.Transform(c, Matrix.CreateRotationY(MathHelper.ToRadians(45)));
			d = Vector3.Transform(d, Matrix.CreateRotationY(MathHelper.ToRadians(45)));

			a += pos;
			b += pos;
			c += pos;
			d += pos;

			Vector3 e = new Vector3(xMin.X, xMin.Y, xMax.Z) + new Vector3(-xMax.X, 0, -xMax.Z);
			Vector3 f = new Vector3(xMax.X, xMin.Y, xMax.Z) + new Vector3(-xMax.X, 0, -xMax.Z);
			Vector3 g = new Vector3(xMax.X, xMax.Y, xMax.Z) + new Vector3(-xMax.X, 0, -xMax.Z);
			Vector3 h = new Vector3(xMin.X, xMax.Y, xMax.Z) + new Vector3(-xMax.X, 0, -xMax.Z);

			e = Vector3.Transform(e, Matrix.CreateRotationY(MathHelper.ToRadians(90 + 45)));
			f = Vector3.Transform(f, Matrix.CreateRotationY(MathHelper.ToRadians(90 + 45)));
			g = Vector3.Transform(g, Matrix.CreateRotationY(MathHelper.ToRadians(90 + 45)));
			h = Vector3.Transform(h, Matrix.CreateRotationY(MathHelper.ToRadians(90 + 45)));

			e += pos;
			f += pos;
			g += pos;
			h += pos;

			Vector2 atx = new Vector2(uvFar.X, uvFar.Y);
			Vector2 btx = new Vector2(uvNear.X, uvFar.Y);
			Vector2 ctx = new Vector2(uvNear.X, uvNear.Y);
			Vector2 dtx = new Vector2(uvFar.X, uvNear.Y);

			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(a, Color.White, atx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(b, Color.White, btx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(c, Color.White, ctx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(d, Color.White, dtx, new Vector3(0, 0, 1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(b, Color.White, btx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(a, Color.White, atx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(d, Color.White, dtx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(c, Color.White, ctx, new Vector3(0, 0, -1)));

            offset = vertices.Count;
            indices.Add(offset + 0);
            indices.Add(offset + 1);
            indices.Add(offset + 3);
            indices.Add(offset + 1);
            indices.Add(offset + 2);
            indices.Add(offset + 3);

            vertices.Add(new VertexPositionColorTextureNormal(e, Color.White, atx, new Vector3(-1, 0, 0)));
            vertices.Add(new VertexPositionColorTextureNormal(f, Color.White, btx, new Vector3(-1, 0, 0)));
            vertices.Add(new VertexPositionColorTextureNormal(g, Color.White, ctx, new Vector3(-1, 0, 0)));
            vertices.Add(new VertexPositionColorTextureNormal(h, Color.White, dtx, new Vector3(-1, 0, 0)));

            offset = vertices.Count;
            indices.Add(offset + 0);
            indices.Add(offset + 1);
            indices.Add(offset + 3);
            indices.Add(offset + 1);
            indices.Add(offset + 2);
            indices.Add(offset + 3);

            vertices.Add(new VertexPositionColorTextureNormal(f, Color.White, btx, new Vector3(1, 0, 0)));
            vertices.Add(new VertexPositionColorTextureNormal(e, Color.White, atx, new Vector3(1, 0, 0)));
            vertices.Add(new VertexPositionColorTextureNormal(h, Color.White, dtx, new Vector3(1, 0, 0)));
            vertices.Add(new VertexPositionColorTextureNormal(g, Color.White, ctx, new Vector3(1, 0, 0)));
        }
    }
}
