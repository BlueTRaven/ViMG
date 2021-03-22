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

		public virtual void Update(double deltaTime)
		{

		}

		public virtual void Initialize(World world)
		{
			this.world = world;
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
	}
}
