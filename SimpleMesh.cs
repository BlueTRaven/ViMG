using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ViMG
{
	public class SimpleMesh<TVert, TIndex> where TVert : struct, IVertexType where TIndex : struct
	{
		private VertexBuffer vbo;
		private IndexBuffer ibo;

		protected GraphicsDevice device;

		private int vertexCount;
		public int VertexCount => vertexCount;
		private int indexCount;
		public int IndexCount => indexCount;

		public Texture2D texture;

		protected bool isEmpty;
		public bool IsEmpty => isEmpty;

		public static SimpleMesh<TVert, TIndex> Empty { get; private set; }

		private bool uploaded;
		public bool Uploaded => uploaded;

		private class DelayedUpload 
		{
			public List<TVert> vertices;
			public List<TIndex> indices;
		}

		private DelayedUpload toUpload;

		static SimpleMesh()
		{
			Empty = new SimpleMesh<TVert, TIndex>();
		}

		public SimpleMesh(GraphicsDevice device, List<TVert> vertices, List<TIndex> indices)
		{
			this.device = device;

			if (Thread.CurrentThread == Main.MainThread)
				Upload(vertices, indices);
			else toUpload = new DelayedUpload()
			{
				vertices = vertices,
				indices = indices
			};
		}

		public void Upload()
		{
			if (toUpload != null)
			{
				Upload(toUpload.vertices, toUpload.indices);
			}
		}

		private void Upload(List<TVert> vertices, List<TIndex> indices)
		{
			if (uploaded)
				throw new Exception("Cannot upload twice.");

			vbo = new VertexBuffer(device, typeof(TVert), vertices.Count, BufferUsage.WriteOnly);
			vbo.SetData(vertices.ToArray());

			ibo = new IndexBuffer(device, typeof(TIndex), indices.Count, BufferUsage.WriteOnly);
			ibo.SetData(indices.ToArray());

			vertexCount = vertices.Count;
			indexCount = indices.Count;

			uploaded = true;
		}

		public SimpleMesh(GraphicsDevice device, List<TVert> vertices, List<TIndex> indices, Texture2D texture) : this(device, vertices, indices)
		{
			this.texture = texture;
		}

		protected SimpleMesh()
		{
			isEmpty = true;
		}

		public bool Use()
		{
			if (isEmpty || !uploaded)
				return false;

			if (toUpload != null)
			{
				Upload(toUpload.vertices, toUpload.indices);
				toUpload = null;
			}

			device.SetVertexBuffer(vbo);
			device.Indices = ibo;

			return true;
		}

		public virtual void Draw(GraphicsDevice device, Effect effect, Vector3 translation, Vector3 rotation, Vector3 scale, Texture2D overrideTexture = null)
		{
			Draw(device, effect, Matrix.CreateTranslation(translation) *
				Matrix.CreateRotationX(rotation.X) *
				Matrix.CreateRotationY(rotation.Y) *
				Matrix.CreateRotationZ(rotation.Z) *
				Matrix.CreateScale(scale), overrideTexture);
		}

		public virtual void Draw(GraphicsDevice device, BasicEffect effect, Vector3 translation, Vector3 rotation, Vector3 scale, Texture2D overrideTexture = null)
		{
			Draw(device, effect, Matrix.CreateTranslation(translation) *
				Matrix.CreateRotationX(rotation.X) *
				Matrix.CreateRotationY(rotation.Y) *
				Matrix.CreateRotationZ(rotation.Z) *
				Matrix.CreateScale(scale), overrideTexture);
		}

		public virtual void Draw(GraphicsDevice device, Effect effect, Matrix transform, Texture2D overrideTexture = null, RectangleF? sourceRectangle = null)
		{
			if (!Use())
				return;

			bool isDepth = effect.Name == "Effects/depth";

			if (isDepth)
			{
				Main.WVP.SetWorld(transform);

				effect.Parameters["WorldViewProjection"].SetValue(Main.WVP.Get());

				foreach (var pass in effect.CurrentTechnique.Passes)
				{
					pass.Apply();
					device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexCount / 3);
				}

				return;
			}

			Main.WVP.SetWorld(transform);

			effect.Parameters["World"].SetValue(transform);
			effect.Parameters["WorldNormal"].SetValue(Matrix.Transpose(Matrix.Invert(transform)));
			effect.Parameters["WorldViewProjection"].SetValue(Main.WVP.Get());

			//bandaid fix. I guess monogame doesn't correctly flush textures, so I do it manually here.
			//TODO optimize this
			for (int i = 0; i < 16; i++)
			{
				device.Textures[i] = null;
			}

			Texture2D useTexture = texture;

			if (useTexture != null || overrideTexture != null)
			{
				if (overrideTexture != null)
					useTexture = overrideTexture;

				effect.Parameters["Texture"].SetValue(useTexture);
			}

			Texture2D fogHeightMap = Main.assetsManager.GetAsset<Texture2D>("height_fog_map");

			if (fogHeightMap != null)
			{
				device.SamplerStates[1] = Main.clampSS;
				effect.Parameters["TextureHeightFogMap"].SetValue(fogHeightMap);
			}

			if (sourceRectangle != null)
			{
				RectangleF rect = sourceRectangle.Value;

				effect.Parameters["UseSourceRect"].SetValue(1);
				effect.Parameters["SourceRectPos"].SetValue(rect.Position);
				effect.Parameters["SourceRectFarPos"].SetValue(rect.FarPosition);
				effect.Parameters["TextureSize"].SetValue(new Vector2(useTexture.Width, useTexture.Height));
			}
			else effect.Parameters["UseSourceRect"].SetValue(0);

			foreach (var pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexCount / 3);
			}
		}

		public virtual void Draw(GraphicsDevice device, BasicEffect effect, Matrix transform, Texture2D overrideTexture = null)
		{
			if (!Use())
				return;

			//basic effect has to be handled differently due to how it caches its values...
			if (effect is BasicEffect basicEffect)
			{
				basicEffect.World = transform;

				Texture2D useTexture = texture;
				if (useTexture != null || overrideTexture != null)
				{
					if (overrideTexture != null)
						useTexture = overrideTexture;

					basicEffect.TextureEnabled = true;
					basicEffect.Texture = useTexture;
				}
			}

			foreach (var pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexCount / 3);
			}
		}
	}
}
