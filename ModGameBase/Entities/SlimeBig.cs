using BrUtility;
using Engine;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Entities
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.Server)]
    [EntityMeta(0)]
    public class SlimeBig : Entity, IHasStats, ISyncedEntity
    {
		private static VerySimpleMesh mesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("slime");

		private BuffManager buffManager;
        public NoticeHandler<Player> noticeHandler;
		public  AISlime ai;

		private int maxHealth = 16;

		private float alive;
		private Color tintColor;

        public SlimeBig() : this(Vector3.Zero)
        {
            
        }

		public SlimeBig(Vector3 position)
        {
			this.Position = position;

            buffManager = new BuffManager(this);
            noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 6.4f, false);

            ai = new AISlime(new Rectangle3D(new Vector3(-Cube.CUBE_SCALE * 0.75f, 0, -Cube.CUBE_SCALE * 0.75f),
                new Vector3(Cube.CUBE_SCALE * 1.5f)), noticeHandler, buffManager, maxHealth);
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);
		}

		public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

			alive += (float)deltaTime;

            AISlime.Funcs<SlimeBig> funcs = new AISlime.Funcs<SlimeBig> { ai = ai, entity = this };
            funcs.Update(deltaTime);
        }
		
		public override void OnKill()
		{
			base.OnKill();

            EntityItem ent = new EntityItem(Position,
                new Vector3(GlobalState.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5), Cube.CUBE_SCALE * 6.4f,
                    GlobalState.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5)),
                new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("slime_chunk"), GlobalState.random.Next(2, 8), 1));
            world.EntityManager.Add(ent);
        }

		public override void OnUnload()
		{
			base.OnUnload();

            AISlime.Funcs<SlimeBig> funcs = new AISlime.Funcs<SlimeBig> { ai = ai, entity = this };
            funcs.OnUnload();
        }

		public Stats GetStats()
		{
			return new Stats()
			{
				HP = ai.Health,
				MaximumHP = maxHealth,

				TintColor = tintColor,
			};
		}

		public void SetStats(Stats stats)
		{
			ai.Health = stats.HP;
			ai.MaxHealth = stats.MaximumHP;
			tintColor = stats.TintColor;

			if (stats.HP <= 0 || stats.MaximumHP <= 0)
				world.EntityManager.Kill(this);
		}

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

            GetSyncedEntity(out var state);
            state.OnSave(saveBytes);

            ai?.OnSave(saveBytes);
            SaveHelper.SaveInt32(saveBytes, maxHealth);
        }

        public override void OnLoad(World world, byte[] loadBytes, in int version)
        {
            base.OnLoad(world, loadBytes, version);

            int index = 0;
            var bs = new SyncedEntity();
            bs.OnLoad(loadBytes, ref index);
            Position = bs.position;
            ai.Velocity = bs.velocity;
            ai.Health = bs.health;

            ai?.OnLoad(loadBytes, ref index);
            maxHealth = SaveHelper.LoadInt32(loadBytes, ref index);
        }

        public void GetSyncedEntity(out SyncedEntity state)
        {
            SyncedEntity aiState = new SyncedEntity();
            ai?.GetSyncedEntity(out aiState);
            aiState.position = Position;
            aiState.rotation = Quaternion.Identity;
            state = aiState;
        }
    }
}
