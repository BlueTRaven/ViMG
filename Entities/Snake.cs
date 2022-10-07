using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using BrUtility;
using Microsoft.Xna.Framework.Graphics;

namespace ViMG.Entities
{
    public class Snake : Entity, IHitboxOwner
    {
		private static (VertexBuffer VBO, IndexBuffer IBO) mesh2x1;
		private static (VertexBuffer VBO, IndexBuffer IBO) mesh1x1;
		private static (VertexBuffer VBO, IndexBuffer IBO) mesh2x2;

		public enum State
		{
			Normal,
			Attack,
			AttackStun
		}

		public Vector3 MaxVelocity = new Vector3(Cube.CUBE_SCALE * 1.5f, Cube.CUBE_SCALE * 17, Cube.CUBE_SCALE * 1.5f);
		public Vector3 Velocity;
		private NoticeHandler<Player> noticeHandler;
		private float idleTimer;
		private float idleMoveTimer;
		private int idleMovements;
		private Vector2 idleDirection;
		private Vector2 idleHome;

		private State state;

		private float attackTimer;

		public float InvulnTimer;

		private bool onGround;
		private bool shouldJump;

		private int touchDamage = 2;
		private int attackDamage = 4;
		private int health = 16;
		private int maxHealth = 16;

		public float AttackStunTime = 1.65f;    //the amount of time the shooter waits in the attacking state after attacking before returning to the normal state
		public float AttackCooldownTime = 2f;   //the amount of time before the shooter can enter the attacking state again
		public float AttackLockTime = 0.25f;    //the amount of time the shooter spends in the attack state before it can begin moving again.

		public float ShootSpeed = Cube.CUBE_SCALE * 16;
		public float MoveSpeed = Cube.CUBE_SCALE;

		public float MoveTowardsTargetDistance = Cube.CUBE_SCALE * 1.75f;
		public float AttackTargetDistance = Cube.CUBE_SCALE * 1.75f;

		private bool isInRangeOfTarget;

		private Rectangle3D touchBounds;
		private int touchHitbox = -1;
		private Rectangle3D attackBounds;
		private int attackHitbox = -1;

		private float alive;

		public Snake()
        {

        }

		public Snake(Vector3 position)
        {
			this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

			noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);

			touchBounds = new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f, Cube.CUBE_SCALE * 0.50f, Cube.CUBE_SCALE * 0.35f),
				new Vector3(Cube.CUBE_SCALE * 0.7f, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 0.7f));

			attackBounds = new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 2f), new Vector3(Cube.CUBE_SCALE * 4));
		}

		public override void Update(double deltaTime)
		{
			alive += (float)deltaTime;

			InvulnTimer -= (float)deltaTime;

			if (touchHitbox == -1)
				touchHitbox = world.HitboxManager.Add(this, touchBounds.Offset(Position), Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_BOTH, touchDamage, 1f, InvulnTimer <= 0);
			else world.HitboxManager.Update(touchHitbox, touchBounds.Offset(Position), InvulnTimer <= 0);

			Vector3 actualMaxVel = MaxVelocity;

			Velocity.Y += World.GRAVITY;

			noticeHandler.Update(deltaTime);

			isInRangeOfTarget = false;

			if (InvulnTimer <= 0 && onGround)
			{
				if (shouldJump)
				{
					Velocity.Y = Cube.CUBE_SCALE * 10;
					shouldJump = false;
				}

				if (noticeHandler.Noticed)
				{
					idleMovements = 0;

					if (state == State.Normal)
					{
						Vector3 dir = noticeHandler.GetNoticedEntity().Position - Position;
						dir.Normalize();

						float distance = XZDistance(noticeHandler.GetNoticedEntity().Position, Position);

						if (distance > MoveTowardsTargetDistance)
						{
							Velocity.X += dir.X;
							Velocity.Z += dir.Z;
						}
						else
						{
							Velocity.X *= 0.95f;
							Velocity.Z *= 0.95f;
						}

						if (distance < AttackTargetDistance)
						{
							isInRangeOfTarget = true;

							attackTimer -= (float)deltaTime;

							if (attackTimer <= 0)
							{
								attackTimer = AttackLockTime;
								state = State.Attack;
							}
						}
					}
					else if (state == State.Attack)
					{
						isInRangeOfTarget = true;

						Velocity.X *= 0.95f;
						Velocity.Z *= 0.95f;

						attackTimer -= (float)deltaTime;

						if (attackTimer <= 0)
						{
							Vector3 dir = (noticeHandler.GetNoticedEntity().Position - new Vector3(0, Cube.CUBE_SCALE, 0)) - Position;
							if (attackHitbox == -1)
								attackHitbox = world.HitboxManager.Add(this, attackBounds.Offset(Position + Vector3.Normalize(dir) * Cube.CUBE_SCALE / 2f), 
									Vector3.Normalize(Velocity), HitboxManager.Group.ENEMYHOSTILE_BOTH, attackDamage, 1);

							state = State.AttackStun;
							attackTimer = AttackStunTime;
						}
					}
					else if (state == State.AttackStun)
					{
						if (attackHitbox != -1)
                        {
							world.HitboxManager.Remove(attackHitbox);
							attackHitbox = -1;
                        }

						isInRangeOfTarget = true;

						Velocity.X *= 0.5f;
						Velocity.Z *= 0.5f;

						attackTimer -= (float)deltaTime;

						if (attackTimer <= 0)
						{
							state = State.Normal;
							attackTimer = AttackCooldownTime;
						}
					}
				}
				else
				{
					state = State.Normal;
					attackTimer = AttackCooldownTime;

					idleTimer -= (float)deltaTime;

					if (idleTimer <= 0)
						idleMoveTimer -= (float)deltaTime;

					if (idleMovements == 0 && idleTimer <= 0 && idleMoveTimer <= 0)
					{
						idleHome = new Vector2(Position.X, Position.Z);

						idleTimer = Main.random.NextFloat(4f, 12f);
						idleMoveTimer = Main.random.NextFloat(0.25f, 2f);
						idleMovements = Main.random.Next(2, 6);

						idleDirection = Main.random.NextAngle();
					}
					else
					{
						float distFromIdleHome = (new Vector2(Position.X, Position.Z) - idleHome).Length();

						if (distFromIdleHome > Cube.CUBES_PER_UNIT * 16)
							idleDirection = -idleDirection;

						if (idleTimer <= 0 && idleMoveTimer <= 0)
						{
							idleMovements--;
							idleDirection = Main.random.NextAngle();
							idleMoveTimer = Main.random.NextFloat(0.25f, 2f);
						}
					}

					if (idleTimer <= 0)
					{
						Velocity.X += idleDirection.X;
						Velocity.Z += idleDirection.Y;
					}
					else
					{
						Velocity.X *= 0.85f;
						Velocity.Z *= 0.85f;
					}
				}

				Vector2 clampXY = new Vector2(actualMaxVel.X, actualMaxVel.Z);
				Vector2 velXY = new Vector2(Velocity.X, Velocity.Z);

				if (velXY.Length() > clampXY.Length())
				{
					velXY.Normalize();
					velXY *= clampXY.Length();
				}

				Velocity = new Vector3(velXY.X, Velocity.Y, velXY.Y);
			}

			if (Velocity.Y < -actualMaxVel.Y)
				Velocity.Y = -actualMaxVel.Y;

			Position += Velocity * (float)deltaTime;

			onGround = false;
			shouldJump = false;
			UpdateCollision();

			if ((world.player.Position - Position).Length() > 128 * Cube.CUBE_SCALE)
				world.EntityManager.Remove(this);
		}

		private void UpdateCollision()
		{
			const int checkSize = 1;

			for (int x = -checkSize; x <= checkSize; x++)
			{
				for (int y = -checkSize; y <= checkSize; y++)
				{
					for (int z = -checkSize; z <= checkSize; z++)
					{
						CubePosition pos = CubePosition.FromWorldSpace(Position) + new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace); //CubePosition.FromWorldSpace(Position);

						if (world.GetChunkManager().IsInWorldBounds(pos) &&
							world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air).Collision != Cube.CollisionValue.None)
						{
							Rectangle3D cubeBounds = CubePosition.BoundsWorldSpace(pos);

							Vector3 offset = new Vector3(0, Cube.CUBE_SCALE * 0.25f, 0);
							Vector3 checkPos = Position + offset;

							if (CollisionHelper.CheckCollision(cubeBounds, checkPos, Cube.CUBE_SCALE * 0.25f, out Vector3 change))
							{
								Position = (checkPos - offset) + change;

								if (change.Y > 0)
								{
									Velocity.Y = 0;
									onGround = true;
								}
								else if (change.Y < 0)
									Velocity.Y = 0;
								else if (change.X != 0)
									Velocity.X = 0;
								else if (change.Z != 0)
									Velocity.Z = 0;
							}
						}
					}
				}
			}

			if (onGround && state == State.Normal && InvulnTimer <= 0)
			{
				if (Velocity.Length() > Cube.CUBE_SCALE / 4f)
				{
					var ray = world.RaycastVector(Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0), new Vector3(Velocity.X, 0, Velocity.Z), Cube.CUBE_SCALE * 2,
						(Vector3 pos) =>
						{
							Cube cube = world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air);

							return cube.Collision != Cube.CollisionValue.None;
						});

					if (ray.hasHit)
					{
						shouldJump = true;
					}
				}
			}

			foreach (Snake otherEntity in world.EntityManager.GetAll<Snake>())
			{
				if (otherEntity != this)
				{
					Vector2 distXZ = new Vector2(Position.X, Position.Z) - new Vector2(otherEntity.Position.X, otherEntity.Position.Z);

					if (distXZ.Length() < Cube.CUBE_SCALE)
					{
						Vector2 correctPos = new Vector2(otherEntity.Position.X, otherEntity.Position.Z) + Vector2.Normalize(distXZ) * Cube.CUBE_SCALE;

						Position = new Vector3(correctPos.X, Position.Y, correctPos.Y);
					}
				}
			}
		}

		public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
		{
			if (InvulnTimer <= 0)
			{
				if (other.group == HitboxManager.Group.PLAYER_DEAL)
				{
					Vector3 direction = Vector3.Normalize(other.direction);

					Velocity = new Vector3(direction.X * 3.2f * Cube.CUBE_SCALE, 6.4f * Cube.CUBE_SCALE, direction.Z * 3.2f * Cube.CUBE_SCALE);

					health -= other.damage;

					if (health <= 0)
					{
						health = 0;
						world.EntityManager.Remove(this);

						if (touchHitbox != -1)
							world.HitboxManager.Remove(touchHitbox);
					}

					InvulnTimer = 0.25f;
					attackTimer = 0;    //immediately attempt to attack?

					noticeHandler.OnTakeDamage(other.owner as Player);
				}
			}
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			base.Draw(device, effect);

			if (mesh2x1.VBO == null)
			{
				mesh2x1 = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE);
				mesh1x1 = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);
				mesh2x2 = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2);
			}

			(VertexBuffer VBO, IndexBuffer IBO) useMesh = mesh2x1;

			Vector3 velXZ = new Vector3(Velocity.X, 0, Velocity.Z);
			velXZ.Normalize();

			float facingDotCamera = Vector3.Dot(velXZ, -Main.camera.Forward);

			//Facing within 45 degrees of the camera.
			bool isFacingCamera = facingDotCamera < MathHelper.ToRadians(45);

			RectangleF sourceRect = new RectangleF(0, 0, 32, 16);

			if (isFacingCamera)
			{
				sourceRect = new RectangleF(0, 16, 16, 16);
				useMesh = mesh1x1;
			}

			if (state == State.Normal)
			{
				if (Velocity.Length() > Cube.CUBE_SCALE * 0.1f)
				{
					float animP = (alive % 0.75f) / 0.75f;

					int frame = (int)(animP * 2f);

					sourceRect.x += sourceRect.width * frame;
				}
			}
			else if (state == State.Attack)
            {
				useMesh = mesh2x2;
				sourceRect.y = 32;
				sourceRect.width = 32;
				sourceRect.height = 32;
				const int NUM_FRAMES = 4;

				int frame = (int)((1 - (attackTimer / AttackLockTime)) * NUM_FRAMES);

				sourceRect.x = 32 * frame;
            }

			Vector3 tintColor = InvulnTimer > 0 ? Color.Red.ToVector3() : Color.White.ToVector3();

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("snake"),
				DrawHelper.BlackPixel, DrawHelper.BlackPixel, useMesh.VBO, useMesh.IBO,
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(Position), sourceRect, tintColor));

			DrawHelper3D.DrawHealthbar(device, health, maxHealth, Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0));
		}

		private static float XZDistance(Vector3 otherPosition, Vector3 position)
        {
			return (new Vector2(otherPosition.X, otherPosition.Z) - new Vector2(position.X, position.Z)).Length();
        }
	}
}
