using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Entities
{
    public class SnakeFlying : Entity, IHitboxOwner
    {
		public enum State
		{
			Normal,
			Attack,
			AttackStun
		}

		private static (VertexBuffer VBO, IndexBuffer IBO) mesh2x2;

		private State state;

		public float InvulnTimer;

		public Vector3 Velocity;
        public float MaxVelocity = Cube.CUBE_SCALE * 6;

        public float Acceleration = Cube.CUBE_SCALE / 2f;

		public float MoveTowardsTargetDistance = Cube.CUBE_SCALE * 2f;
		public float AttackTargetDistance = Cube.CUBE_SCALE * 2f;

		public float AttackStunTime = 1.65f;    //the amount of time the ai waits in the attacking state after attacking before returning to the normal state
		public float AttackCooldownTime = 2f;   //the amount of time before the ai can enter the attacking state again
		public float AttackLockTime = 0.25f;    //the amount of time the ai spends in the attack state before it can begin moving again.
		public float AttackHitboxTime = 6f / 60f;
		private float attackTimer;

		public int Health;
		public int MaxHealth = 10;

		public int TouchDamage = 2;
		private Rectangle3D touchHitboxBounds;
		private int touchHitbox = -1;

		public int AttackDamage = 4;
		private Rectangle3D attackHitboxBounds;
		private int attackHitbox = -1;

		private float alive;

		public NoticeHandler<Player> noticeHandler;

		public SnakeFlying()
        {
        }

        public SnakeFlying(Vector3 position)
        {
            this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

			Health = MaxHealth;

			touchHitboxBounds = new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
				new Vector3(Cube.CUBE_SCALE * 0.7f, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 0.7f));
			attackHitboxBounds = new Rectangle3D(-new Vector3(Cube.CUBE_SCALE), new Vector3(Cube.CUBE_SCALE * 2f));
			noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);
		}

		public override void Update(double deltaTime)
		{
			base.Update(deltaTime);

			alive += (float)deltaTime;

			if (touchHitbox == -1)
				touchHitbox = world.HitboxManager.Add(this, touchHitboxBounds.Offset(Position), Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_BOTH, TouchDamage, 1f, InvulnTimer <= 0);
			else world.HitboxManager.Update(touchHitbox, touchHitboxBounds.Offset(Position), InvulnTimer <= 0);

			noticeHandler.Update(deltaTime);

			if (noticeHandler.Noticed)
			{
				Vector3 playerDir = noticeHandler.Target.Position - Position;
				float distance = playerDir.Length();
				playerDir.Normalize();

				if (state == State.Normal)
				{
					if (distance > MoveTowardsTargetDistance)
					{
						if (Velocity.Length() > 0)
						{
							Vector3 velocityDir = Vector3.Normalize(Velocity);
							float velocityLen = Velocity.Length();

							Vector3 cross = Vector3.Cross(velocityDir, playerDir);
							Matrix mat = Matrix.CreateFromAxisAngle(cross, MathHelper.ToRadians(5));

							Vector3 rotated = Vector3.Normalize(Vector3.Transform(velocityDir, mat));
							Velocity = rotated * (velocityLen + Acceleration);
						}
						else
						{
							//Initial first acceleration (we start with 0 velocity, and that results in NaNs, so we kinda have to seed it)
							Velocity += playerDir * Acceleration;
						}
					}
					else
					{
						//slow down very fast.
						Velocity *= 0.65f;

						if (distance < AttackTargetDistance)
						{
							attackTimer -= (float)deltaTime;

							if (attackTimer <= 0)
							{
								attackTimer = AttackLockTime;
								state = State.Attack;
							}
						}
					}
				}
				else if (state == State.Attack)
				{
					Velocity *= 0.95f;

					attackTimer -= (float)deltaTime;

					if (attackTimer <= 0)
					{
						Vector3 dir = (noticeHandler.GetNoticedEntity().Position - new Vector3(0, Cube.CUBE_SCALE, 0)) - Position;
						if (attackHitbox == -1)
							attackHitbox = world.HitboxManager.Add(this, attackHitboxBounds.Offset(Position + Vector3.Normalize(dir) * Cube.CUBE_SCALE * 1.5f),
								Vector3.Normalize(Velocity), HitboxManager.Group.ENEMYHOSTILE_BOTH, AttackDamage, 1);

						state = State.AttackStun;
						attackTimer = AttackStunTime;
					}
				}
				else if (state == State.AttackStun)
				{
					if (attackTimer <= AttackStunTime - AttackHitboxTime)
					{
						if (attackHitbox != -1)
						{
							world.HitboxManager.Remove(attackHitbox);
							attackHitbox = -1;
						}
					}

					Velocity *= 0.95f;

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
				//TODO wander behavior
            }

            if (Velocity.Length() > MaxVelocity)
                Velocity = Vector3.Normalize(Velocity) * MaxVelocity;

            Position += Velocity * (float)deltaTime;

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

								if (change.Y != 0)
									Velocity.Y = -Velocity.Y * 0.5f;
								else if (change.X != 0)
									Velocity.X = -Velocity.X * 0.5f;
								else if (change.Z != 0)
									Velocity.Z = -Velocity.Z * 0.5f;
							}
						}
					}
				}
			}

			foreach (SnakeFlying otherEntity in world.EntityManager.GetAll<SnakeFlying>())
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

					Health -= other.damage;

					if (Health <= 0)
					{
						Health = 0;
						world.EntityManager.Remove(this);

						if (touchHitbox != -1)
							world.HitboxManager.Remove(touchHitbox);
					}

					//buffManager.AddBuffs(other.applyBuffs);

					InvulnTimer = 0.25f;
					//attackTimer = 0;    //immediately attempt to attack?

					noticeHandler.OnTakeDamage(other.owner as Player);
				}
				else if ((us.group & HitboxManager.Group.ENEMYHOSTILE_DEAL) == HitboxManager.Group.ENEMYHOSTILE_DEAL && other.group == HitboxManager.Group.PLAYER_TAKE &&
					other.canInteract)
                {
					//Bounce off the player if we deal contact damage to them
					Velocity = -Velocity;
                }
			}
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			base.Draw(device, effect);

			if (mesh2x2.VBO == null)
			{
				mesh2x2 = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2);
			}

			RectangleF sourceRectSnake = new RectangleF(0, 34, 32, 32);

			if (state == State.Attack)
            {
				const int ATT_NUM_FRAMES = 4;
				int frame = (int)((1 - (attackTimer / AttackLockTime)) * ATT_NUM_FRAMES);
				sourceRectSnake = new RectangleF(32 * frame, 34, 32, 32);
			}

			RectangleF sourceRectWings = new RectangleF(0, 64, 32, 32);

			const int WINGS_NUM_FRAMES = 3;
			int wingFrame = (int)((1 - ((alive % 0.25f) / 0.25f)) * WINGS_NUM_FRAMES);
			sourceRectWings.x = 32 * wingFrame;

			Vector3 tintColor = InvulnTimer > 0 ? Color.Red.ToVector3() : Color.White.ToVector3();

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("snake"),
				DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh2x2.VBO, mesh2x2.IBO,
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(Position), sourceRectWings, tintColor));

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("snake"),
				DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh2x2.VBO, mesh2x2.IBO,
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(Position), sourceRectSnake, tintColor));

			DrawHelper3D.DrawHealthbar(device, Health, MaxHealth, Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0));
		}
	}
}
