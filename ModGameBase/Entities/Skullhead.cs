using BrUtility;
using Engine;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Entities
{
	// TODO refactor multiplayer
	// Add some sort of targeting mechanism. Right now targets only player index 0
	[EntityMeta(0)]
	[EntitySerializable(EntitySerializableAttribute.SerializationType.Server)]
    public class Skullhead : Entity, IHitboxOwner, IHasStats, ISyncedEntity
    {
		public enum State
        {
			Chase,
			SetupDash,
			Dash,
			Rotate,
			SlowChase,
			Transition,
			PostTransitionWait,
        }

		public static class Constants
		{
            public const float CHASE_TIME = 1f;//6f;
            public const float SETUPDASH_TIME = 3f;
            public const float DASH_TIME = 0.75f;
            public const float ROTATE_TIME = 1.4f; //or, in other words, time between each skull fire
            public const float SLOW_CHASE_TIME = 8f;
            public const float TRANSITIONP2_TIME = 3f;
            public const float POSTTRANSITIONWAIT_TIME = 1.3f;

            public const float CHASE_DISTANCE = Cube.CUBE_SCALE * 12f;
            public const float DASH_DISTANCE = Cube.CUBE_SCALE * 8f;
            public const float SLOWCHASE_DISTANCE = Cube.CUBE_SCALE * 1.5f;
        }

        public Color tintColor = Color.White;

		public float invulnTimer;
		private float alive;
		public State state;
		public float stateTimer;
		private float stateTime;
		private int stateCounter;

		private Vector3 velocity;

		private float TRAIN_RADIUS = Cube.CUBE_SCALE;
		public Vector3[] trainPositions = new Vector3[8];

		private Rectangle3D bounds = new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 2f), new Vector3(Cube.CUBE_SCALE * 4));
		private int hitbox = -1;
		private int health;
		private int maxHealth = 300;
		private int statMaxHealth;

		private float kbScale = 1.5f;

		public bool transitioned = false;

		private NoticeHandler<Player> notice;
		private Vector3 targetOffset;
		private Vector3 targetPosition;

		private BuffManager buffManager;

		private ProjectileManager.ProjectileBatchStats batchStats;
		private ProjectileManager.ProjectileStats stats;
		private int visStatsId;

		public Skullhead() 
		{
			notice = new NoticeHandler<Player>(this, float.MaxValue, false);

            health = maxHealth;

            buffManager = new BuffManager(this);

            state = State.Chase;
            stateTimer = Constants.CHASE_TIME;
            stateTime = Constants.CHASE_TIME;

            batchStats = new ProjectileManager.ProjectileBatchStats(3, new float[3] { -15f, 0, 15f }, null);
            stats = new ProjectileManager.ProjectileStats(HitboxManager.Group.ENEMYHOSTILE_DEAL, 3, 1f, Cube.CUBE_SCALE / 4, Cube.CUBE_SCALE);
            visStatsId = GlobalState.Registry.ProjectileRegistry.Get("skullhead_skull").Id;
        }

		public Skullhead(Vector3 position) : this()
        {
			this.Position = position;
            Array.Fill(trainPositions, Position);
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            world.ChatManager.AddChatMessage("Skullhead has awoken!", Color.Orange);
		}

        public override void OnKill()
        {
            base.OnKill();

			int which = GlobalState.random.Next(0, 5);

			Items.ItemInstance drop;
			if (which == 0)
				drop = new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("bow_bowner"), 1, 1);
			else if (which == 1)
				drop = new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("sword_runic_bone"), 1, 1);
			else if (which == 2)
				drop = new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("magic_bone_staff"), 1, 1);
			else if (which == 3)
				drop = new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("heart_ossified"), 1, 1);
			else if (which == 4)
				drop = new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("bone_whistle"), 1, 1);
			else drop = new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("item_dirt"), 1, 1);

			EntityItem ent = new EntityItem(Position, 
				new Vector3(GlobalState.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5), Cube.CUBE_SCALE * 6.4f,
					GlobalState.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5)), drop);
			world.EntityManager.Add(ent);

			ent = new EntityItem(Position,
				new Vector3(GlobalState.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5), Cube.CUBE_SCALE * 6.4f,
					GlobalState.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5)),
				new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("brittle_infused_bone"), GlobalState.random.Next(4, 20), 1));
			world.EntityManager.Add(ent);

			if (!world.WorldInfo.flags.Flags.HasFlag(WorldLogics.WorldFlags.FlagValues.SKULLHEAD_DEAD))
			{
				world.WorldInfo.flags.Flags |= WorldLogics.WorldFlags.FlagValues.SKULLHEAD_DEAD;
				world.ChatManager.AddChatMessage("The lava layer has receded.", Color.Yellow);
			}
			
			world.ChatManager.AddChatMessage("Skullhead has been defeated!", Color.Orange);
		}

        public override void OnUnload()
        {
            base.OnUnload();

			if (hitbox != -1)
				world.HitboxManager.Remove(hitbox);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

			if (hitbox == -1)
				hitbox = world.HitboxManager.Add(this, bounds.Offset(Position), Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_BOTH, 6, 1f, invulnTimer <= 0);
			else world.HitboxManager.Update(hitbox, bounds.Offset(Position).ToOBB(), invulnTimer <= 0);

			invulnTimer -= (float)deltaTime;
			alive += (float)deltaTime;

			buffManager.Update(deltaTime);

			if (state == State.Chase)
			{
                targetPosition = world.GetClosestPlayer(Position)?.Position ?? Vector3.Zero;
                Vector3 direction = targetPosition - Position;

				stateTimer -= (float)deltaTime;

				if (direction.Length() > Constants.CHASE_DISTANCE)
				{
                    EntityHelper.AddCappedVelocity(ref velocity, Vector3.Normalize(direction) * Cube.CUBE_SCALE,
                        new Vector3(Cube.CUBE_SCALE * 5.5f));
                    //velocity += Vector3.Normalize(direction * Cube.CUBE_SCALE);
				}
				else
				{
					velocity *= 0.85f;

					if (stateTimer <= 0)
					{
						state = State.SetupDash;
						stateTimer = Constants.SETUPDASH_TIME;
						stateTime = Constants.SETUPDASH_TIME;

						targetOffset = -Vector3.Normalize(new Vector3(direction.X, 0, direction.Z)) * Constants.DASH_DISTANCE;
					}
				}

				//ClampVelocityLength(Cube.CUBE_SCALE * 16);
			}
			else if (state == State.SetupDash)
            {
				targetPosition = world.GetClosestPlayer(Position)?.Position ?? Vector3.Zero;

				Vector3 ground = world.ChunkManager.CubeView.GetFirstSolidDown(
					CubePosition.FromWorldSpace(new Vector3(Position.X, Position.Y + Cube.CUBE_SCALE * 16, Position.Z)))
					.GetOrDefault(new CubePosition(0, 0, 0, CubePosition.CoordinateSpace.CubeSpace)).InWorldSpace() +
					new Vector3(0, Cube.CUBE_SCALE * 4, 0);

				Vector3 offsetPosition = targetPosition + targetOffset;

				offsetPosition.Y = MathF.Max(offsetPosition.Y, ground.Y);

				Vector3 direction = offsetPosition - Position;
				stateTimer -= (float)deltaTime;

				if (direction.Length() > Cube.CUBE_SCALE / 2f)
				{
					velocity += Vector3.Normalize(direction * Cube.CUBE_SCALE);
				}
				else
				{
					velocity *= 0.5f;

					if (stateTimer <= 0)
                    {
						state = State.Dash;
						stateTimer = Constants.DASH_TIME;
						stateTime = Constants.DASH_TIME;

						targetOffset = -targetOffset;
						//targetPosition = targetPosition - targetOffset;
                    }
				}

				ClampVelocityLength(Cube.CUBE_SCALE * 16);
			}
			else if (state == State.Dash)
            {
                targetPosition = world.GetClosestPlayer(Position)?.Position ?? Vector3.Zero;

                Vector3 direction = (targetPosition + targetOffset) - Position;

				//Vector3 direction = targetPosition - Position;
				bool inRange;
				stateTimer -= (float)deltaTime;

				if (direction.Length() > Cube.CUBE_SCALE / 2f)
				{
					velocity = Vector3.Normalize(direction) * Cube.CUBE_SCALE * 20f;
					inRange = false;
				}
				else inRange = true;

				if (inRange || stateTimer <= 0)
				{
					state = State.Rotate;
					stateTimer = Constants.ROTATE_TIME;
					stateTime = Constants.ROTATE_TIME;

					stateCounter = GlobalState.random.NextCoinFlip() ? -1 : 1;
				}

				ClampVelocityLength(Cube.CUBE_SCALE * 32f);
			}
			else if (state == State.Rotate)
            {
                targetPosition = world.GetClosestPlayer(Position)?.Position ?? Vector3.Zero;

                Vector3 ground = world.ChunkManager.CubeView.GetFirstSolidDown(
					CubePosition.FromWorldSpace(new Vector3(Position.X, Position.Y + Cube.CUBE_SCALE * 16, Position.Z)))
					.GetOrDefault(new CubePosition(0, 0, 0, CubePosition.CoordinateSpace.CubeSpace)).InWorldSpace() +
					new Vector3(0, Cube.CUBE_SCALE * 4, 0);

				Vector3 offsetPosition = targetPosition + targetOffset;

				offsetPosition.Y = MathF.Max(offsetPosition.Y, ground.Y);

				Vector3 direction = offsetPosition - Position;

				stateTimer -= (float)deltaTime;

				float offsetDegrees = MathF.Sign(stateCounter) < 0 ? -0.65f : 0.65f;

				targetOffset = Vector3.Transform(targetOffset, Matrix.CreateRotationY(MathHelper.ToRadians(offsetDegrees)));

				if (direction.Length() > Cube.CUBE_SCALE * 2.5f)
				{
					velocity += Vector3.Normalize(direction * Cube.CUBE_SCALE * 1f);
				}
				else
				{
					velocity *= 0.5f;
				}

				if (stateTimer <= 0)
				{
					if (Math.Abs(stateCounter) >= 4)
                    {
						state = State.SlowChase;
						stateTimer = Constants.SLOW_CHASE_TIME;
						stateTime = Constants.SLOW_CHASE_TIME;

						stateCounter = 0;
					}
                    else
                    {
						stateCounter += Math.Sign(stateCounter);
						stateTimer = Constants.ROTATE_TIME;

						world.ProjectileManager.AddBatch(this, Position, Vector3.Normalize(targetPosition - Position) * Cube.CUBE_SCALE * 12f, 4f,
							batchStats, visStatsId, stats);
                    }
				}

				ClampVelocityLength(Cube.CUBE_SCALE * 64f);
			}
			else if (state == State.SlowChase)
            {
                targetPosition = world.GetClosestPlayer(Position)?.Position ?? Vector3.Zero;
                Vector3 direction = targetPosition - Position;

				stateTimer -= (float)deltaTime;

				if (direction.Length() > Constants.SLOWCHASE_DISTANCE)
				{
					//if we get knocked back too far away, clamp velocity
					if (direction.Length() > Cube.CUBE_SCALE * 16 * 2.5f)
						velocity = Vector3.Normalize(velocity) * Cube.CUBE_SCALE * 5.5f;

					EntityHelper.AddCappedVelocity(ref velocity, Vector3.Normalize(direction) * Cube.CUBE_SCALE / 4f,
						new Vector3(Cube.CUBE_SCALE * 5.5f));
				}
				
				if (direction.Length() < Constants.SLOWCHASE_DISTANCE || stateTimer <= 4f)
				{
					//velocity *= 0.98f;

					if (stateTimer <= 0)
					{
						state = State.Chase;
						stateTimer = Constants.CHASE_TIME;
						stateTime = Constants.CHASE_TIME;
					}
				}

				//ClampVelocityLength(Cube.CUBE_SCALE * 5.5f);
			}
			else if (state == State.Transition)
			{
                stateTimer -= (float)deltaTime;

                velocity *= 0.9f;

				kbScale = 0;

				if (stateTimer <= 0)
				{
					state = State.PostTransitionWait;
                    stateTimer = Constants.POSTTRANSITIONWAIT_TIME;
                    stateTime = Constants.POSTTRANSITIONWAIT_TIME;
					transitioned = true;

                    for (int i = 0; i < 8; i++)
                        world.EntityManager.Add(new SkullheadEye(Position - Vector3.Left * Cube.CUBE_SCALE * 5f, this));
                }
			}
			else if (state == State.PostTransitionWait)
			{
                stateTimer -= (float)deltaTime;
				velocity = Vector3.Zero;

                if (stateTimer <= 0)
                {
					kbScale = 1;	//more resistance to knockback

                    state = State.Chase;
                    stateTimer = Constants.CHASE_TIME;
                    stateTime = Constants.CHASE_TIME;
                }
            }

			if (world.GetClosestPlayer(Position)?.Health <= 0)
            {
				//limit max upwards velocity so other forces can't prevent us from moving downwards.
				if (velocity.Y > -Cube.CUBE_SCALE)
					velocity.Y = -Cube.CUBE_SCALE;

				velocity.Y -= Cube.CUBE_SCALE * 32;

				if (world.DistanceFromPlayer(Position) > Cube.CUBE_SCALE * 128f)
                {
					world.EntityManager.Kill(this);
                }
            }

			for (int i = 0; i < trainPositions.Length; i++)
			{
				ref Vector3 trainPos = ref trainPositions[i];
				trainPos += new Vector3(0, World.GRAVITY * 16 * (float)deltaTime, 0);

				Vector3 lastPosition = i == 0 ? Position - new Vector3(0, Cube.PIXEL_SCALE * 64, 0) : trainPositions[i - 1];

				Vector3 dist = trainPos - lastPosition;

				if (dist.Length() > TRAIN_RADIUS)
				{
					trainPos = lastPosition + Vector3.Normalize(dist) * TRAIN_RADIUS;
				}
			}

			Position += velocity * (float)deltaTime;
        }

		private void ClampVelocityLength(float maxVel)
        {
			if (velocity.Length() > maxVel)
				velocity = Vector3.Normalize(velocity) * maxVel;
		}

		public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
		{
			if (invulnTimer <= 0)
			{
				if (other.group == HitboxManager.Group.PLAYER_DEAL)
				{
					EntityHelper.CalculateKnockback(ref velocity, other, kbScale);
					//Vector3 direction = Vector3.Normalize(other.direction);

					//velocity = direction * Cube.CUBE_SCALE * 3f * other.knockback;

					health -= other.damage;

					if (health <= 0)
					{
						health = 0;
						world.EntityManager.Kill(this);

						if (hitbox != -1)
							world.HitboxManager.Remove(hitbox);
					}

					if (state != State.Transition && !transitioned && (float)health / (float)maxHealth <= 0.3f)
					{
						state = State.Transition;
						stateTime = Constants.TRANSITIONP2_TIME;
						stateTimer = stateTime;

						//velocity = Vector3.Normalize(other.direction) * Cube.CUBE_SCALE * 8f;
					}

					buffManager.AddBuffs(other.applyBuffs);

					invulnTimer = 0.25f;
				}
			}
		}

		public Stats GetStats()
		{
			return new Stats()
			{
				HP = health,
				MaximumHP = maxHealth,

				TintColor = Color.White,
			};
		}

		public void SetStats(Stats stats)
		{
			health = stats.HP;
			statMaxHealth = stats.MaximumHP;
			tintColor = stats.TintColor;

			if (stats.HP <= 0 || stats.MaximumHP <= 0)
				world.EntityManager.Kill(this);
		}

        public void GetSyncedEntity(out SyncedEntity state)
        {
			state = new SyncedEntity
			{
				position = Position,
				velocity = velocity,
			};
        }
    }
}
