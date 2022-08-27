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
		public bool Active = true;				//An entity is INACTIVE when the chunk that contains it unloads.
		public bool CanBecomeInactive = true;	//Certain entity types (bosses, etc) may wish to never become inactive.
		public bool DestroyOnInactive = true;	//Most entity types will be destroyed upon becoming inactive by default.

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
