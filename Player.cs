using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Items;

namespace ViMG
{
	public class Player
	{
		public const float INTERACT_DISTANCE = Cube.CUBE_SCALE * 4.5f;

		private enum State
		{
			Noclip,
			Normal
		}

		public Vector3 Position;
		public Vector3 Velocity;

		private float moveSpeed = 8;
		public Vector3 MaxVelocity = new Vector3(64, 340, 64);
		public Vector3 MaxVelocityRunning = new Vector3(128, 340, 128);
		public float MaxFallVelocity;

		private MouseState currentMS;
		private MouseState originalMS;

		private State state;
		private World world;

		private bool onGround;

		private Rectangle3D Bounds => new Rectangle3D(Position + new Vector3(-Cube.CUBE_SCALE * 0.85f / 2f, -Cube.CUBE_SCALE * 2f, -Cube.CUBE_SCALE * 0.85f / 2f),
			new Vector3(Cube.CUBE_SCALE * 0.85f, Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 0.85f));
		private int hurtbox = -1;
		private float invulnTimer;
		private float inputLockupTimer;

		private int hitbox = -1;
		private Vector3 damageDir;
		private float hitboxTimer;
		private float hitboxSize;
		private const float HITBOX_TIME = 3f / Main.FIXED_FPS;
		private float attackTimer;
		private const float ATTACK_TIME = 0.5f;

		private World.RaycastResult lookAtResult;
		private CubePosition lookAtPos;
		private CubePosition placeAtPos;
		private float lookAtColSine;
		private const float lookAtColTimeMax = 0.5f;
		private float lookAtColTimer = lookAtColTimeMax;

		private SimpleMesh<VertexPositionColor, int> lookAtMesh;
		private SimpleMesh<VertexPositionColorTextureNormal, int> itemMesh;

		public List<Item> items = new List<Item>();
		public int currentItem;

		public Player(World world)
		{
			this.world = world;

			Position = new Vector3(world.sizeInCubes * Cube.CUBE_SCALE / 2f, world.sizeInCubes * Cube.CUBE_SCALE, world.sizeInCubes * Cube.CUBE_SCALE / 2f);

			state = State.Noclip;
			Mouse.SetPosition(Main.WindowResolution.X / 2, Main.WindowResolution.Y / 2);
			originalMS = Mouse.GetState();

			items.Add(new ItemSwordBase());
			items.Add(new ItemPickaxeBase());
			items.Add(new ItemCube(world.CubeRegistry.Get(1), 1));
			items.Add(new ItemCube(world.CubeRegistry.Get(3), 3));
		}

		public void Update(double deltaTime)
		{
			if (world.IsInWorldBounds(Position) && (world.GetChunkManager().GetChunk(ChunkPosition.WorldSpaceChunk(Position)) == null || !world.GetChunkManager().GetChunk(ChunkPosition.WorldSpaceChunk(Position)).Initialized))
				return;

			if (hurtbox == -1)
				hurtbox = world.HitboxManager.Add(Bounds, Vector3.Zero, 0);
			else world.HitboxManager.Update(hurtbox, Bounds);

			UpdateMovement(deltaTime);

			if (invulnTimer <= 0)
			{
				DenseHitboxArray.Hitbox[] hitboxes = world.HitboxManager.GetAll();
				for (int i = 0; i < world.HitboxManager.Capacity; i++)
				{
					ref DenseHitboxArray.Hitbox hitbox = ref hitboxes[i];

					if (hitbox.active)
					{
						if (hitbox.group == 1)
						{
							if (hitbox.bounds.Intersects(Bounds))
							{
								Vector3 direction = Vector3.Normalize(Bounds.Center - hitbox.bounds.Center);

								Velocity = new Vector3(direction.X * 128, 128, direction.Z * 128);

								inputLockupTimer = 1;
								invulnTimer = 0.25f;
							}
						}
					}
				}
			}

			if (hitbox != -1)
			{
				if (hitboxTimer <= 0)
				{
					world.HitboxManager.Remove(hitbox);
					hitbox = -1;
				}
				else
				{
					Rectangle3D rect = new Rectangle3D(Position + damageDir - new Vector3(hitboxSize / 2), new Vector3(hitboxSize));

					world.HitboxManager.Update(hitbox, rect);
				}
			}

			if (Main.inputManager.JustPressed(Keys.G))
			{
				if (state == State.Noclip)
					state = State.Normal;
				else if (state == State.Normal)
					state = State.Noclip;
			}

			if (Main.inputManager.JustPressed(Keys.V))
			{
				world.ProjectileManager.Add(new Entities.ProjectileManager.Projectile(Position, -Main.camera.Forward * 0, 4, 20, Main.assetsManager.GetAsset<Texture2D>("bullet"), 1, true));
			}

			if (Main.inputManager.JustPressed(Keys.D1))
			{
				currentItem = 0;
			}

			if (Main.inputManager.JustPressed(Keys.D2))
			{
				currentItem = 1;
			}

			if (Main.inputManager.JustPressed(Keys.D3))
			{
				currentItem = 2;
			}

			if (Main.inputManager.JustPressed(Keys.D4))
			{
				currentItem = 3;
			}

			if (attackTimer <= 0)
			{
				if (Main.inputManager.IsPressed(A1r.Input.MouseInput.LeftButton))
				{
					if (items[currentItem].LeftClick(this, -Main.camera.Forward))
						PerformAction();
				}

				if (Main.inputManager.IsPressed(A1r.Input.MouseInput.RightButton))
				{
					if (items[currentItem].RightClick(this, -Main.camera.Forward))
						PerformAction();
				}
			}

			lookAtResult = world.Raycast(-Main.camera.Position, -Main.camera.Position - Main.camera.Forward * INTERACT_DISTANCE,
			(Vector3 pos) =>
			{
				return world.IsInWorldBounds(pos) && world.GetRaw(pos) != 0;
			});

			if (lookAtResult.hasHit)
			{
				if (world.IsInWorldBounds(lookAtResult.hit))
				{
					this.lookAtPos = CubePosition.FromWorldSpace(lookAtResult.hit);
					this.placeAtPos = CubePosition.FromWorldSpace(lookAtResult.hit + CubePosition.ToWorldSpaceV3(lookAtResult.normal));

					/*if (Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton))
					{
						List<CubePosition> positions = world.GetAdjacentsInWorld(lookAtPos);
						world.MineCube(lookAtPos);
						foreach (CubePosition pos in positions)
							world.MineCube(pos);
						//Chunk chunk = world.GetChunkManager().GetChunk(lookAtPos);
						//chunk.GetData().SetCube(lookAtPos, 0);
					}

					if (world.IsInWorldBounds(placeAtPos) && Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton))
					{
						Chunk chunk = world.GetChunkManager().GetChunk(placeAtPos);
						chunk.GetData().SetCube(placeAtPos, 2);
					}*/
				}
			}

			UpdateMouse();

			Main.camera.Position = -Position;

			for (int x = -world.DrawDistanceHoriz; x <= world.DrawDistanceHoriz; x++)
			{
				for (int y = -world.DrawDistanceVert; y <= world.DrawDistanceVert; y++)
				{
					for (int z = -world.DrawDistanceHoriz; z < world.DrawDistanceHoriz; z++)
					{
						ChunkPosition chunkPos = ChunkPosition.WorldSpaceChunk(-Main.camera.Position);
						chunkPos.X += x;
						chunkPos.Y += y;
						chunkPos.Z += z;

						if (world.IsInWorldBounds(chunkPos))
						{
							if (!world.GetChunkManager().IsChunkGenerated(chunkPos))
							{
								world.GetChunkManager().MarkGenerateDirty(chunkPos);
							}
						}
					}
				}
			}

			invulnTimer -= (float)deltaTime;
			inputLockupTimer -= (float)deltaTime;
			hitboxTimer -= (float)deltaTime;
			attackTimer -= (float)deltaTime;
			lookAtColTimer += (float)deltaTime;
			lookAtColSine = (float)(Math.Sin(2 * Math.PI * ((lookAtColTimer % lookAtColTimeMax) / lookAtColTimeMax)) + 1f) / 2f;
		}

		private void UpdateMovement(double deltaTime)
		{
			if (state == State.Noclip)
			{
				if (Main.inputManager.JustPressed(Keys.K))
				{
					Position = Vector3.Zero;
				}

				const float MIN_CAM_SPEED = 512;
				const float MAX_CAM_SPEED = MIN_CAM_SPEED * 2;

				float moveSpeed = MIN_CAM_SPEED;

				if (Main.inputManager.IsHeld(Keys.LeftShift))
					moveSpeed = MAX_CAM_SPEED;

				if (Main.inputManager.IsPressed(Keys.W))
					Position -= Vector3.Normalize(Main.camera.Forward) * moveSpeed * (float)deltaTime;
				if (Main.inputManager.IsPressed(Keys.S))
					Position += Vector3.Normalize(Main.camera.Forward) * moveSpeed * (float)deltaTime;
				if (Main.inputManager.IsPressed(Keys.A))
					Position -= Vector3.Normalize(Main.camera.Right) * moveSpeed * (float)deltaTime;
				if (Main.inputManager.IsPressed(Keys.D))
					Position += Vector3.Normalize(Main.camera.Right) * moveSpeed * (float)deltaTime;
			}
			else if (state == State.Normal)
			{
				Vector3 actualMaxVel = MaxVelocity;

				bool movementPressed = false;
				bool running = false;

				if (inputLockupTimer <= 0)
				{
					if (Main.inputManager.IsHeld(Keys.LeftShift))
						running = true;

					if (running)
						actualMaxVel = MaxVelocityRunning;

					if (Main.inputManager.IsPressed(Keys.W))
					{
						Velocity -= Vector3.Normalize(Main.camera.ForwardYawOnly) * moveSpeed;
						movementPressed = true;
					}
					if (Main.inputManager.IsPressed(Keys.S))
					{
						Velocity += Vector3.Normalize(Main.camera.ForwardYawOnly) * moveSpeed;
						movementPressed = true;
					}
					if (Main.inputManager.IsPressed(Keys.A))
					{
						Velocity -= Vector3.Normalize(Main.camera.Right) * moveSpeed;
						movementPressed = true;
					}
					if (Main.inputManager.IsPressed(Keys.D))
					{
						Velocity += Vector3.Normalize(Main.camera.Right) * moveSpeed;
						movementPressed = true;
					}
					if (onGround && Main.inputManager.JustPressed(Keys.Space))
					{
						Velocity.Y = MaxVelocity.Y;
						onGround = false;
					}

					Vector2 clampXY = new Vector2(actualMaxVel.X, actualMaxVel.Z);
					Vector2 velXY = new Vector2(Velocity.X, Velocity.Z);

					if (velXY.Length() > clampXY.Length())
					{
						velXY.Normalize();
						velXY *= clampXY.Length();
					}

					if (!movementPressed && onGround)
					{
						if (velXY.Length() > 0)
						{
							velXY = Vector2.Normalize(velXY) * velXY.Length() * 0.85f;
						}
					}

					Velocity = new Vector3(velXY.X, Velocity.Y, velXY.Y);
				}
				else
				{
					if (onGround)
					{
						Vector2 velXY = new Vector2(Velocity.X, Velocity.Z);

						if (velXY.Length() > 0)
						{
							velXY = Vector2.Normalize(velXY) * velXY.Length() * 0.85f;
						}

						Velocity = new Vector3(velXY.X, Velocity.Y, velXY.Y);
					}
				}

				Velocity.Y += World.GRAVITY;
				if (Velocity.Y > actualMaxVel.Y)
					Velocity.Y = actualMaxVel.Y;

				Position += Velocity * (float)deltaTime;

				onGround = false;
				UpdateCollision();
				/*for (int x = -1; x <= 1; x++)
				{
					for (int y = -1; y <= 1; y++)
					{
						for (int z = -1; z <= 1; z++)
						{
							Vector3 pos = Position + new Vector3(x, y, z) * Cube.CUBE_SCALE;
							if (world.IsInWorldBounds(pos) && world.GetRaw(pos) != 0)
							{
								Rectangle3D cubeBounds = new Rectangle3D(CubePosition.RoundToCubeSpace(Position) + (new Vector3(x, y, z) * Cube.CUBE_SCALE), new Vector3(Cube.CUBE_SCALE));
								Rectangle3D playerBounds = bounds.Offset(Position);

								if (cubeBounds.Intersects(playerBounds))
								{
									Position.Y += cubeBounds.Top - playerBounds.Bottom;
									Velocity.Y = 0;
									onGround = true;
								}
							}
						}
					}
				}*/
			}
		}

		private Vector2[] offsetsDown = new Vector2[4]
		{
			new Vector2(-0.325f) * Cube.CUBE_SCALE,
			new Vector2(-0.325f, 0.325f) * Cube.CUBE_SCALE,
			new Vector2(0.325f, -0.325f) * Cube.CUBE_SCALE,
			new Vector2(0.325f) * Cube.CUBE_SCALE
		};

		private Vector3[] directions = new Vector3[4]
		{
			new Vector3(-1, 0, 0),
			new Vector3(1, 0, 0),
			new Vector3(0, 0, -1),
			new Vector3(0, 0, 1)
		};

		private void UpdateCollision()
		{
			const float height = Cube.CUBE_SCALE * 2f;
			for (int i = 0; i < 4; i++)
			{
				Vector3 startPos = new Vector3(Position.X + offsetsDown[i].X, Position.Y - height, Position.Z + offsetsDown[i].Y);
				Vector3 dir = new Vector3(0, height, 0);
				var resultDown = world.RaycastVector(startPos, dir, height, (Vector3 pos) =>
				{
					return world.IsInWorldBounds(pos) && world.GetRaw(pos) != 0;
				});

				if (resultDown.hasHit)
				{
					CubePosition pos = CubePosition.FromWorldSpace(resultDown.hit);

					Position.Y = pos.Y * Cube.CUBE_SCALE + Cube.CUBE_SCALE + height;
					Velocity.Y = 0;
					onGround = true;
				}
			}

			const float sideWidth = 0.45f;

			for (int i = 0; i < 4; i++)
			{
				Vector3 startPos = new Vector3(Position.X, Position.Y - height + Cube.CUBE_SCALE * 0.5f, Position.Z);
				ref Vector3 dir = ref directions[i];
				var resultSideBot = world.RaycastVector(startPos, dir, Cube.CUBE_SCALE * sideWidth, (Vector3 pos) =>
				{
					return world.IsInWorldBounds(pos) && world.GetRaw(pos) != 0;
				});

				if (resultSideBot.hasHit)
				{
					CubePosition pos = CubePosition.FromWorldSpace(resultSideBot.hit);

					Vector3 offset = resultSideBot.hit - directions[i] * Cube.CUBE_SCALE * sideWidth;

					Position = new Vector3(offset.X, Position.Y, offset.Z);

					if (dir.X > 0 || dir.X < 0)
						Velocity.X = 0;
					if (dir.Z > 0 || dir.Z < 0)
						Velocity.Z = 0;
				}
			}
		}

		private void UpdateMouse()
		{
			currentMS = Mouse.GetState();

			if (currentMS != originalMS)
			{
				float scalar = 0.25f;

				Vector3 camRotation = Main.camera.Rotation;

				Vector2 delta = new Vector2(originalMS.X, originalMS.Y) - new Vector2(currentMS.X, currentMS.Y);
				camRotation.Y -= MathHelper.ToRadians(delta.X) * scalar;
				camRotation.X -= MathHelper.ToRadians(delta.Y) * scalar;

				if (camRotation.X > MathHelper.ToRadians(89))
					camRotation.X = MathHelper.ToRadians(89);
				else if (camRotation.X < -MathHelper.ToRadians(89))
					camRotation.X = -MathHelper.ToRadians(89);

				Main.camera.Rotation = camRotation;
			}
		}

		public void PerformAction()
		{
			attackTimer = ATTACK_TIME;
		}

		public void SpawnHitbox()
		{
			const float hitboxSize = Cube.CUBE_SCALE * 1.75f;

			if (hitbox != -1)
				world.HitboxManager.Remove(hitbox);

			damageDir = -Main.camera.Forward * (hitboxSize + 10f);

			Rectangle3D rect = new Rectangle3D(Position + damageDir - new Vector3(hitboxSize / 2), new Vector3(hitboxSize));
			this.hitboxSize = hitboxSize;

			hitbox = world.HitboxManager.Add(rect, -Main.camera.Forward, 2);

			hitboxTimer = HITBOX_TIME;

			attackTimer = ATTACK_TIME;
		}

		public void Draw(GraphicsDevice device)
		{
			if (lookAtMesh == null)
			{
				lookAtMesh = MeshHelper.MakeCubeVertexPositionColor(device, Vector3.Zero, new Vector3(Cube.CUBE_SCALE), MeshHelper.CubeFace.ALL, Color.White, DrawHelper.WhitePixel);
			}

			if (lookAtResult.hasHit && world.IsInWorldBounds(lookAtResult.hit))
			{
				device.DepthStencilState = Main.genericDSS;
				device.RasterizerState = Main.wireframeRS;
				Main.BasicEffect.DiffuseColor = Color.Lerp(Color.Transparent, Color.Red, lookAtColSine).ToVector3();
				lookAtMesh.Draw(device, Main.BasicEffect, Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 2f)) *
					Matrix.CreateScale(1.126f) *
					Matrix.CreateTranslation(new Vector3(Cube.CUBE_SCALE / 2f)) * 
					Matrix.CreateTranslation(lookAtPos.InWorldSpace(null)));

				//DrawHelper3D.DrawCubeImmediate(device, lookAtPos.InWorldSpace(null), new Vector3(Cube.CUBE_SCALE), );
				//world.DrawWireframeUnscaled(device, lookAtResult.hit, Vector3.One);
				//world.DrawWireframeCube(device, lookAtPos.InWorldSpace(null), Color.Red);
				//world.DrawWireframeCube(device, placeAtPos);
			}

			device.DepthStencilState = Main.nodepthDSS;
			device.RasterizerState = Main.noCullRS;

			if (itemMesh == null)
			{
				Vector3 min = -new Vector3(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE, 0);
				Vector3 max = new Vector3(Cube.CUBE_SCALE / 2f, 0, 0);

				Vector3 a = new Vector3(max.X, min.Y, max.Z);
				Vector3 b = new Vector3(min.X, min.Y, max.Z);
				Vector3 c = new Vector3(min.X, max.Y, max.Z);
				Vector3 d = new Vector3(max.X, max.Y, max.Z);

				List<VertexPositionColorTextureNormal> vertices = new List<VertexPositionColorTextureNormal>();
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

				vertices.Add(new VertexPositionColorTextureNormal(a, Color.White, atx, new Vector3(0, 0, 1)));
				vertices.Add(new VertexPositionColorTextureNormal(b, Color.White, btx, new Vector3(0, 0, 1)));
				vertices.Add(new VertexPositionColorTextureNormal(c, Color.White, ctx, new Vector3(0, 0, 1)));
				vertices.Add(new VertexPositionColorTextureNormal(d, Color.White, dtx, new Vector3(0, 0, 1)));

				//Texture2D tex = Main.assetsManager.GetAsset<Texture2D>("swrod");
				itemMesh = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices);
			}

			items[currentItem].Draw(device, this, -Main.camera.Forward);
			
			//itemMesh.Draw(device, Main.CubeEffect,
				//GetHeldMatrix(), items[currentItem].Texture, items[currentItem].SourceRect);
		}

		public void DrawUI(SpriteBatch batch)
		{
			const int padding = 8;
			const int margin = 8;

			const int scale = 4;

			for (int i = 0; i < items.Count; i++)
			{
				Item item = items[i];

				if (item != null)
				{
					Vector2 pos = new Vector2(margin + i * 16 * scale + i * padding * scale, margin);
					if (currentItem == i)
					{
						batch.DrawRectangle(new RectangleF(pos, 16 * scale, 16 * scale), Color.Gray * 0.25f);
						batch.DrawHollowRectangle(new RectangleF(pos, 16 * scale, 16 * scale), 4, Color.Black);
					}

					batch.Draw(item.Texture, pos, item.SourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
				}
			}
		}

		public Matrix GetHeldMatrix()
		{
			float percent = attackTimer / ATTACK_TIME;

			if (attackTimer <= 0)
				percent = 0;

			return Matrix.CreateRotationX(-Main.camera.Rotation.X) *
					Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
					Matrix.CreateRotationZ(-Main.camera.Rotation.Z) *
					Matrix.CreateRotationX(-MathHelper.ToRadians(35) * percent) *
					Matrix.CreateTranslation(Position - Main.camera.Forward * Cube.CUBE_SCALE + Main.camera.Right * 16);
		}

		public void DrawDebug(GraphicsDevice device)
		{
			if (state == State.Noclip)
			{
				DrawHelper3D.DrawAxesImmediate(device, Position - Main.camera.Forward * 40);

				if (hitbox != -1 && world.HitboxManager.Get(hitbox).active)
					DrawHelper3D.DrawCubeImmediate(device, world.HitboxManager.Get(hitbox).bounds.Position, world.HitboxManager.Get(hitbox).bounds.Size, Color.Red);
			}
		}

		public World GetWorld()
		{
			return world;
		}
	}
}
