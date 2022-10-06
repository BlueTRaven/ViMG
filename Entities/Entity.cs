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

		public bool Dead = false;
		public bool AlwaysRender;
		//An entity becomes INACTIVE once it is serialized. It is unloaded and removed from the entity list.
		public bool CanBecomeInactive = true;	//Certain entity types (bosses, etc) may wish to never become inactive.
		public bool DestroyOnInactive = true;   //Most entity types will be destroyed upon becoming inactive by default.

		//Force the entity to be serialized.
		//Note that this does not guarantee an entity will be properly serialized. Entities without properly implemented OnSave/OnLoad methods may be
		//corrupted or in invalid state when deserialized. (They will also typically spawn at 0, 0, 0, which is a problem!)
		public bool Serialize = false;

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
			Dead = true;
		}

		public virtual void Draw(GraphicsDevice device, Effect effect)
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
