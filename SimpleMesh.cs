using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ViMG
{
	public class SimpleMesh<TVert, TIndex> where TVert : struct, IVertexType where TIndex : struct
	{
		public VertexBuffer VBO;
		public IndexBuffer IBO;

		private string name;
		public string Name 
		{
			get { return name; }
			set
            {
				name = value;
				VBO.Name = name + " vbo";
				IBO.Name = name + " ibo";
            }
		}
		//private int vertexCount;
		public int VertexCount => VBO.VertexCount;
		//private int indexCount;
		public int IndexCount => IBO.IndexCount;

		public Texture2D texture;

		public bool IsEmpty => VBO == null || VBO.VertexCount == 0;
		public bool Uploaded => !IsEmpty;

		private List<TVert> vertices;
		private List<TIndex> indices;

		public static SimpleMesh<TVert, TIndex> Empty { get; private set; }

		static SimpleMesh()
		{
			Empty = new SimpleMesh<TVert, TIndex>();
		}

		public SimpleMesh(GraphicsDevice device, List<TVert> vertices, List<TIndex> indices)
		{
			//TODO: opengl doesn't support multithreaded uploading.
			//IF we end up supporting opengl (not sure we will)
			//then this will cause issues as we have to explicitly call Upload
			if (Thread.CurrentThread == Main.MainThread || Main.CAN_MULTITHREAD_UPLOAD)
				Upload(device, vertices, indices);
			else UploadLater(vertices, indices);
		}

		private void Upload(GraphicsDevice device, List<TVert> vertices, List<TIndex> indices)
		{
			//don't attempt to upload if we've exited or the device is lost.
			if (Main.Exit || device.IsDisposed)
				return;

			if (Uploaded)
				throw new Exception("Cannot upload twice.");

			//Stopwatch watch = Stopwatch.StartNew();

			VBO = new VertexBuffer(device, typeof(TVert), vertices.Count, BufferUsage.WriteOnly);
			VBO.SetData(vertices.ToArray());

			IBO = new IndexBuffer(device, typeof(TIndex), indices.Count, BufferUsage.WriteOnly);
			IBO.SetData(indices.ToArray());

			//vertexCount = vertices.Count;
			//indexCount = indices.Count;

			//watch.Stop();

			//Console.WriteLine("Uploaded mesh in " + watch.Elapsed.TotalSeconds + "s");
		}

		public void Upload(GraphicsDevice device)
        {
			Upload(device, vertices, indices);
			vertices = null;
			indices = null;
        }

		public SimpleMesh(GraphicsDevice device, List<TVert> vertices, List<TIndex> indices, Texture2D texture) : this(device, vertices, indices)
		{
			this.texture = texture;
		}

		protected SimpleMesh()
		{
		}

		protected virtual void UploadLater(List<TVert> vertices, List<TIndex> indices)
		{
			this.vertices = vertices;
			this.indices = indices;
		}

		public bool Use(GraphicsDevice device)
		{
			//Disables Forward renderer
			//return false;

			if (IsEmpty)
				return false;

			device.SetVertexBuffer(VBO);
			device.Indices = IBO;

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
			if (!Use(device))
				return;

			/*if (effect.Name == "Effects/depth")
            {
				DrawDepth(device, effect, transform);
				return;
            }*/

			//bandaid fix. I guess monogame doesn't correctly flush textures, so I do it manually here.
			//TODO optimize this
			for (int i = 0; i < 16; i++)
			{
				device.Textures[i] = null;
			}

			Main.WVP.SetWorld(transform);

			effect.Parameters["World"].SetValue(transform);
			effect.Parameters["WorldNormal"].SetValue(Matrix.Transpose(Matrix.Invert(transform)));
			effect.Parameters["WorldViewProjection"].SetValue(Main.WVP.Get());

			Texture2D useTexture = texture;

			if (useTexture != null || overrideTexture != null)
			{
				if (overrideTexture != null)
					useTexture = overrideTexture;

				effect.Parameters["Texture"].SetValue(useTexture);
			}

			if (sourceRectangle != null)
			{
				RectangleF rect = sourceRectangle.Value;

				effect.Parameters["UseSourceRect"].SetValue(true);
				effect.Parameters["SourceRectPos"].SetValue(rect.Position);
				effect.Parameters["SourceRectFarPos"].SetValue(rect.FarPosition);
				effect.Parameters["TextureSize"].SetValue(new Vector2(useTexture.Width, useTexture.Height));
			}
			else effect.Parameters["UseSourceRect"].SetValue(false);

			foreach (var pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexCount / 3);
			}
		}

		public virtual void DrawDepth(GraphicsDevice device, Effect effect, Matrix transform, Matrix viewProj)
        {
			if (!Use(device))
				return;

			//Main.WVP.SetWorld(transform);

			Matrix wvp = transform * viewProj;

			effect.Parameters["WorldViewProjection"].SetValue(wvp);

			foreach (var pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexCount / 3);
			}
		}

		public virtual void DrawDebugVertexPositionColor(GraphicsDevice device, Effect vertexPositionColorDebugEffect, Color color, Matrix transform)
        {
			if (!Use(device))
				return;

			Main.WVP.SetWorld(transform);

			vertexPositionColorDebugEffect.Parameters["DiffuseColor"].SetValue(color.ToVector4());
			vertexPositionColorDebugEffect.Parameters["WorldViewProjection"].SetValue(Main.WVP.Get());

			foreach (var pass in vertexPositionColorDebugEffect.CurrentTechnique.Passes)
			{
				pass.Apply();
				device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexCount / 3);
			}
		}

		public virtual void DrawDebugVertexPositionTexture(GraphicsDevice device, Effect vertexPositionTextureDebugEffect, Color diffuseColor, Matrix transform, Texture overrideTexture = null)
        {
			if (!Use(device))
				return;

			Main.WVP.SetWorld(transform);

			vertexPositionTextureDebugEffect.Parameters["DiffuseColor"].SetValue(diffuseColor.ToVector4());
			vertexPositionTextureDebugEffect.Parameters["WorldViewProjection"].SetValue(Main.WVP.Get());
			vertexPositionTextureDebugEffect.Parameters["Texture"].SetValue(overrideTexture == null ? texture : overrideTexture);

			foreach (var pass in vertexPositionTextureDebugEffect.CurrentTechnique.Passes)
			{
				pass.Apply();
				device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexCount / 3);
			}
		}

		public virtual void Draw(GraphicsDevice device, BasicEffect effect, Matrix transform, Texture2D overrideTexture = null)
		{
			if (!Use(device))
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
