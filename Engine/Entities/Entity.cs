using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ViMG.Cubes;
using ViMG.IMGUIImpl;

namespace ViMG.Entities
{
	public class Entity
	{
		public enum SerializationTime
		{ 
			Never,			//Never serialized. Overrides Serialize.
			OnChunkUnload,	//Only serialized when the chunk is unloaded.
			OnWorldSave,	//Only serialized when the world is saved.
			Always
		}

		public Random? random;
		public Vector3 Position;
		public World world;

		public bool Dead = false;
		public bool AlwaysRender;
		//An entity becomes INACTIVE once it is serialized. It is unloaded and removed from the entity list.
		public bool CanBeDisabled = true;	//Certain entity types (bosses, etc) may wish to never become disabled.
		public bool DestroyOnDisabled = true;   //Most entity types will be destroyed upon becoming disabled by default.

		//Force the entity to be serialized.
		//Note that this does not guarantee an entity will be properly serialized. Entities without properly implemented OnSave/OnLoad methods may be
		//corrupted or in invalid state when deserialized. (They will also typically spawn at 0, 0, 0, which is a problem!)
		public bool Serialize = false;

		private ulong id = 0;
		public ulong Id => id;

		public float Alive;
        public double TimeInitialized = 0;
		public bool IsInitialized => TimeInitialized != 0;

		public bool DoesSync = true;
		public bool DoesMajorSync = true;
		public double TimeSynced;
		public double TimeMajorSynced;
		public double SyncInterval {get; protected set; } = 0.25;
		public double MajorSyncInterval { get; protected set; } = 5;

		public bool Enabled = true;
		public float DisableDistance = Cube.CUBE_SCALE * 128;

		public bool NetEntity = false;
		// Enabled/disabled by network - run when client attempts to delete an entity. Clients cannot (normally) delete entities
		public bool NetEnable = true;

        public void SetId(ulong id)
		{
			this.id = id;
		}

		public virtual void Initialize(World world)
		{
			this.world = world;

            random = new Random((int)Id + Main.Frame);

            TimeInitialized = Main.Time;
		}

		public virtual void LoadContent(World world)
		{
            IMGUIConsole.Assert(!Main.IsHeadless);
		}

		public virtual void Update(double deltaTime)
		{
			Alive += (float)deltaTime;

			random = new Random((int)Id + Main.Frame);
		}

		//Called when an enemy is killed by normal means; I.e. the player has dealt enough damage to it.
		public virtual void OnKill()
		{
			Dead = true;
		}

		//Called in all cases when an enemy is removed, including when it is unloaded.
		public virtual void OnUnload()
        {

        }

		public virtual void Draw(GraphicsDevice device, Effect effect)
		{

		}

		public virtual void OnCubeUpdated(CubePosition updating, int updatedId)
		{

		}

		// TODO: List<byte> to something better. Maybe NetWriter?
		// TODO: include a serialization context. World save or Net save
		public virtual void OnSave(List<byte> saveBytes)
		{

		}

		public virtual void OnLoad(byte[] loadBytes, in int version)
		{

		}
	}
}
