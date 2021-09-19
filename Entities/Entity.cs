using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Entities
{
	public class Entity
	{
		public Vector3 Position;
		public World world;

		public bool AlwaysRender;

		private ulong id = 0;
		public ulong Id => id;

		public void SetId(ulong id)
		{
			this.id = id;
		}

		public virtual void Initialize(World world)
		{
			this.world = world;
		}

		public virtual void Update(double deltaTime)
		{

		}

		public virtual void OnDelete()
		{

		}

		public virtual void Draw(GraphicsDevice device)
		{

		}

		public virtual void OnCubeUpdated(ChunkData updatingParent, CubePosition updating, int updatedId)
		{

		}

		public virtual void OnSave(List<byte> saveBytes)
		{

		}

		public virtual void OnLoad(byte[] loadBytes, in int version)
		{

		}
	}
}
