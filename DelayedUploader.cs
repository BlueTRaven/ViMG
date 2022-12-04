using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ViMG
{
	public class DelayedUploader<TVert, TIndex> where TVert : struct, IVertexType where TIndex : struct
	{
		public class ToUploadLater
		{
			public readonly SimpleMesh<TVert, TIndex> mesh;
			public readonly List<TVert> vertices;
			public readonly List<TIndex> indices;

			public ToUploadLater(SimpleMesh<TVert, TIndex> mesh, List<TVert> vertices, List<TIndex> indices)
			{
				this.mesh = mesh;
				this.vertices = vertices;
				this.indices = indices;
			}
		}

		public Queue<ToUploadLater> meshesToUploadLater = new Queue<ToUploadLater>();

		public void Upload(GraphicsDevice device)
		{
			if (Thread.CurrentThread != Main.MainThread)
				throw new Exception("Cannot call upload on delayed uploader anywhere outside of main thread.");

			while (meshesToUploadLater.Count > 0)
			{
				var toUpload = meshesToUploadLater.Dequeue();
				//toUpload.mesh.Upload(device, toUpload.vertices, toUpload.indices);
			}
		}
	}
}
