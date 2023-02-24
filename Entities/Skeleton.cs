using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG.Entities
{
	public class Skeleton : Entity, IHitboxOwner
	{
		private enum State
        {
			Active,
			LyingInPile,
			LyingInPileKillable
        }

		private static SimpleMesh<VertexCube, int> mesh;

		private State state;

		private NoticeHandler<Player> noticeHandler;

		public Vector3 MaxVelocity = new Vector3(Cube.CUBE_SCALE * 1.5f, Cube.CUBE_SCALE * 17, Cube.CUBE_SCALE * 1.5f);
		public Vector3 Velocity;

		private int health;
		private int maxHealth = 8;

		private bool onGround;
		private bool shouldJump;

		private Rectangle3D Bounds => new Rectangle3D(Position - new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
			new Vector3(Cube.CUBE_SCALE * 0.70f, Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 0.70f));
		private int hitbox = -1;

		private float invulnTimer;
		private float resurrectTimer;
		private float alive;

		private float idleTimer;
		private float idleMoveTimer;
		private int idleMovements;
		private Vector2 idleDirection;
		private Vector2 idleHome;

		private CubePosition trackBoneBlockPosition;

		public Skeleton(Vector3 position)
		{
			this.Position = position;
		}

		public override void Initialize(World world)
		{
			base.Initialize(world);

			noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);

			health = maxHealth;
		}

        public override void OnDelete()
        {
            base.OnDelete();

			EntityItem ent = new EntityItem(Position, new Items.ItemInstance(Main.Registry.ItemRegistry.Get("brittle_bone"), 1, 1));
			ent.Velocity = new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5), Cube.CUBE_SCALE * 6.4f,
				Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5));
			world.EntityManager.Add(ent);
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

			alive += (float)deltaTime;

			invulnTimer -= (float)deltaTime;

			bool hasBoneWhistle = noticeHandler.Target != null && Items.ItemBoneWhistle.HasBoneWhistle(noticeHandler.Target);

			if (hitbox == -1)
				hitbox = world.HitboxManager.Add(this, Bounds, Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_BOTH, 4, 1f);
			else
			{
				if (hasBoneWhistle)
				{
					//if BOTH, replace with TAKE since we're passive now and don't want to deal touch damage.
					if (world.HitboxManager.Get(hitbox).group == HitboxManager.Group.ENEMYHOSTILE_BOTH)
					{
						world.HitboxManager.Remove(hitbox);
						hitbox = world.HitboxManager.Add(this, Bounds, Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_TAKE, 4, 1f);
					}
				}
                else
                {
					//if TAKE, this means we had the bone whistle on previously and must now re-enable the DEAL part of the hitbox (so put it back onto BOTH).
					if (world.HitboxManager.Get(hitbox).group == HitboxManager.Group.ENEMYHOSTILE_TAKE)
					{
						world.HitboxManager.Remove(hitbox);
						hitbox = world.HitboxManager.Add(this, Bounds, Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_BOTH, 4, 1f);
					}
                }

				world.HitboxManager.Update(hitbox, Bounds);
			}

			Vector3 actualMaxVel = MaxVelocity;

			Velocity.Y += World.GRAVITY;

			noticeHandler.Update(deltaTime);

			if (!world.IsNight())
			{
				//every 3ish seconds try to damage
				if (invulnTimer <= 0 && (alive % 3.1f) < 0.1f)
				{
					invulnTimer = 0.25f;
					health -= 1;

					if (health <= 0)
					{
						health = 0;
						if (SearchForNearbyBoneBlocks())
						{
							state = State.LyingInPile;
							resurrectTimer = 20;
						}

						if (state == State.Active || state == State.LyingInPileKillable)
							world.EntityManager.Remove(this);
					}
				}
			}

			if (invulnTimer <= 0 && onGround)
			{
				if (shouldJump)
                {
					Velocity.Y = Cube.CUBE_SCALE * 10;
					shouldJump = false;
                }

				if (state == State.Active)
				{
					if (noticeHandler.Noticed && !hasBoneWhistle)
					{
						idleMovements = 0;

						Vector3 dir = Vector3.Normalize(noticeHandler.GetNoticedEntity().Position - Position) * Cube.CUBE_SCALE;

						Velocity.X += dir.X;
						Velocity.Z += dir.Z;
					}
					else
					{
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
				}
				else
				{
					resurrectTimer -= (float)deltaTime;

					if (resurrectTimer <= 0)
					{
						state = State.Active;
						health = 3;
						maxHealth = 8;
					}

					if (state == State.LyingInPile)
                    {
						if ((trackBoneBlockPosition.InWorldSpace() - Position).Length() > Cube.CUBE_SCALE * 4.5f)
						{
							state = State.LyingInPileKillable;
							resurrectTimer += 4;    //additional 4 seconds if we kill the block.
							health = 4;
							maxHealth = 4;
						}
					}

					Velocity.X *= 0.85f;
					Velocity.Z *= 0.85f;
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

		//TODO performance
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

						if (world.ChunkManager2.IsInWorldBounds(pos) && 
							world.ChunkManager2.ThreadedView.GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air).Collision != Cube.CollisionValue.None)
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

			if (onGround)
			{
				var ray = world.RaycastVector(Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0), new Vector3(Velocity.X, 0, Velocity.Z), Cube.CUBE_SCALE * 2,
					(Vector3 pos) =>
					{
						Cube cube = world.ChunkManager2.ThreadedView.GetCube(CubePosition.FromWorldSpace(pos)).GetOrDefault(Main.Registry.CubeRegistry.Air);

						return cube.Collision != Cube.CollisionValue.None;
					});

				if (ray.hasHit)
				{
					shouldJump = true;
				}
			}

			foreach (Skeleton skeleton in world.EntityManager.GetAll<Skeleton>())
            {
				if (skeleton != this)
                {
					Vector2 distXZ = new Vector2(Position.X, Position.Z) - new Vector2(skeleton.Position.X, skeleton.Position.Z);

					if (distXZ.Length() < Cube.CUBE_SCALE)
					{
						Vector2 correctPos = new Vector2(skeleton.Position.X, skeleton.Position.Z) + Vector2.Normalize(distXZ) * Cube.CUBE_SCALE;

						Position = new Vector3(correctPos.X, Position.Y, correctPos.Y);
					}
				}
            }
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			base.Draw(device, effect);

			if (mesh == null)
				MakeMesh(device);

			//world.DrawWireframeUnscaled(device, Bounds, Color.Red);

			RectangleF sourceRect = new RectangleF(0, 0, 16, 32);

			if (state != State.Active)
				sourceRect = new RectangleF(16, 0, 16, 32);

			Vector3 tintColor = invulnTimer > 0 ? Color.Red.ToVector3() : Color.White.ToVector3();

			Vector3 vibratePos = Vector3.Zero;

			if (state != State.Active && resurrectTimer <= 4 && (resurrectTimer % (4f / 60f)) / (4f / 60f) < 0.25f)
			{
				if (resurrectTimer <= 1)
                {
					vibratePos = new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE / 8f, Cube.CUBE_SCALE / 8f), 0,
						Main.random.NextFloat(-Cube.CUBE_SCALE / 8f, Cube.CUBE_SCALE / 8f));
				}
                else
                {
					vibratePos = new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE / 16f, Cube.CUBE_SCALE / 16f), 0,
						Main.random.NextFloat(-Cube.CUBE_SCALE / 16f, Cube.CUBE_SCALE / 16f));
                }
            }

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(mesh.texture,
				DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(vibratePos) *
				Matrix.CreateTranslation(Position), sourceRect, tintColor));

			if (health < maxHealth)
				DrawHelper3D.DrawHealthbar(device, health, maxHealth, Position);
		}

		private static void MakeMesh(GraphicsDevice device)
		{
			Vector3 min = -new Vector3(Cube.CUBE_SCALE / 2f, 0, 0);
			Vector3 max = new Vector3(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE * 2, 0);

			Vector3 a = new Vector3(max.X, min.Y, max.Z);
			Vector3 b = new Vector3(min.X, min.Y, max.Z);
			Vector3 c = new Vector3(min.X, max.Y, max.Z);
			Vector3 d = new Vector3(max.X, max.Y, max.Z);

			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			Vector2 atx = new Vector2(0, 1);
			Vector2 btx = new Vector2(1, 1);
			Vector2 ctx = new Vector2(1, 0);
			Vector2 dtx = new Vector2(0, 0);

			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(a, Color.White, atx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(b, Color.White, btx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(c, Color.White, ctx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(d, Color.White, dtx, new Vector3(0, 0, 1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(b, Color.White, btx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(a, Color.White, atx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(d, Color.White, dtx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(c, Color.White, ctx, new Vector3(0, 0, -1)));

			mesh = new SimpleMesh<VertexCube, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("skeleton"));
		}

		public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
		{
			if (invulnTimer <= 0)
			{
				if (other.group == HitboxManager.Group.PLAYER_DEAL)
				{
					Vector3 direction = Vector3.Normalize(other.direction);

					Velocity = new Vector3(direction.X * 3.2f * Cube.CUBE_SCALE, 6.4f * Cube.CUBE_SCALE, direction.Z * 3.2f * Cube.CUBE_SCALE);

					health -= other.damage;

					if (health <= 0)
					{
						health = 0;
						if (SearchForNearbyBoneBlocks())
						{
							state = State.LyingInPile;
							resurrectTimer = 20;
						}

						if (state == State.Active || state == State.LyingInPileKillable)
							world.EntityManager.Remove(this);
					}

					invulnTimer = 0.25f;

					noticeHandler.OnTakeDamage(other.owner);
				}
			}
		}

		public override void OnCubeUpdated(CubePosition updating, int updatedId)
		{
			if (state == State.LyingInPile)
            {
				if (updating == trackBoneBlockPosition)
				{
					if (updatedId != Main.Registry.CubeRegistry.Get("brittle_bone_block").Id)
					{
						state = State.LyingInPileKillable;
						resurrectTimer += 4;	//additional 4 seconds if we kill the block.
						health = 4;
						maxHealth = 4;
					}
				}
            }

			base.OnCubeUpdated(updating, updatedId);
		}

		//TODO performance
		private bool SearchForNearbyBoneBlocks()
        {
			Cube boneBlock = Main.Registry.CubeRegistry.Get("brittle_bone_block");

			const int searchRadius = 4;

			for (int x = -searchRadius; x <= searchRadius; x++)
            {
				for (int y = -searchRadius; y <= searchRadius; y++)
                {
					for (int z = -searchRadius; z <= searchRadius; z++)
                    {
						CubePosition checkPos = CubePosition.FromWorldSpace(Position) + new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace);

						if (world.ChunkManager2.ThreadedView.GetCube(checkPos).GetOrDefault(Main.Registry.CubeRegistry.Air) == boneBlock)
                        {
							trackBoneBlockPosition = checkPos;

							return true;
                        }
                    }
				}					
			}

			return false;
        }
	}
}
