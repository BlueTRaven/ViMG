using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Items;

namespace ViMG
{
	public class Player
	{
		public const int GROUP_PLAYER_SOURCE = 2;

		public const float INTERACT_DISTANCE = Cube.CUBE_SCALE * 4.5f;

		private enum State
		{
			Noclip,
			Normal,
			Attack,
			Hurt,
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
		public bool InWater;

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
		private float attackStateTimer;
		private float attackStateMoveTimer;
		private float itemUseCooldownTimer;
		private float useTimer;
		private const float ATTACK_TIME = 0.5f;

		private World.RaycastResult lookAtResult;
		private CubePosition lookAtPos;
		private CubePosition placeAtPos;
		private float lookAtColSine;
		private const float lookAtColTimeMax = 0.5f;
		private float alive = lookAtColTimeMax;

		private const float PULL_RADIUS = 65;
		private const float NEUTRAL_RADIUS = 50;
		private const float PUSH_RADIUS = 35;
		private Vector3 attackStateTargetPos;

		private SimpleMesh<VertexPositionColor, int> lookAtMesh;
		private SimpleMesh<VertexPositionColorTextureNormal, int> itemMesh;

		private Inventory inventory;
		private InventoryInteractorPlayer inventoryInteractor;
		
		private bool inventoryOpen;

		public Player(World world)
		{
			this.world = world;

			Position = new Vector3(world.sizeInCubes * Cube.CUBE_SCALE / 2f, world.sizeInCubes * Cube.CUBE_SCALE, world.sizeInCubes * Cube.CUBE_SCALE / 2f);

			state = State.Noclip;
			Mouse.SetPosition(Main.WindowResolution.X / 2, Main.WindowResolution.Y / 2);
			originalMS = Mouse.GetState();

			inventory = new Inventory(32);
			inventory.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_base"), 1, 1));
			inventory.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("sword_base"), 1, 1));
			inventory.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("iron_chunk"), 4, 1));

			inventory.Set(new ItemInstance(Main.Registry.ItemRegistry.Get("slime_chunk"), 1, 1), 12);
			inventoryInteractor = new InventoryInteractorPlayer(inventory, 4, 8);
		}

		public void Update(double deltaTime)
		{
			if (Main.Debug)
				state = State.Noclip;
			else state = State.Normal;

			if (world.GetChunkManager().IsInWorldBounds(Position) && (world.GetChunkManager().GetChunk(ChunkPosition.WorldSpaceChunk(Position)) == null || !world.GetChunkManager().GetChunk(ChunkPosition.WorldSpaceChunk(Position)).Initialized))
				return;

			if (hurtbox == -1)
				hurtbox = world.HitboxManager.Add(Bounds, Vector3.Zero, 0, -1, -1f);
			else world.HitboxManager.Update(hurtbox, Bounds);

			if (state == State.Noclip)
			{
				UpdateMovement(deltaTime);
			}
			else if (state == State.Normal)
			{
				invulnTimer -= (float)deltaTime;

				UpdateMovement(deltaTime);

				Position += Velocity * (float)deltaTime;

				UpdateCollision();
			}
			else if (state == State.Attack)
			{
				invulnTimer -= (float)deltaTime;

				attackStateTimer -= (float)deltaTime;
				attackStateMoveTimer -= (float)deltaTime;

				UpdateMovement(deltaTime / 2f);

				Position += Velocity * (float)deltaTime;

				UpdateCollision();

				Vector3 dir = attackStateTargetPos - Position;
				if (dir.Length() < PULL_RADIUS)
				{
					if (dir.Length() < NEUTRAL_RADIUS)
					{
						if (dir.Length() < PUSH_RADIUS)
						{
							Position -= Vector3.Normalize(dir) * Math.Min(dir.Length(), 128);
						}
						else
						{
							Velocity.X *= 0.55f;
							Velocity.Z *= 0.55f;
						}
					}
					else
					{
						Velocity = Vector3.Normalize(dir) * 512;
					}
				}
				else
				{
					if (attackStateMoveTimer <= 0)
					{
						Velocity.Y += World.GRAVITY;
						Velocity.X *= 0.55f;
						Velocity.Z *= 0.55f;
					}
				}

				if (attackStateTimer <= 0)
					state = State.Normal;
			}
			else if (state == State.Hurt)
			{
				inputLockupTimer -= (float)deltaTime;

				if (inputLockupTimer <= 0)
					state = State.Normal;
			}

			Vector2 center = new Vector2(world.sizeInCubes * Cube.CUBE_SCALE / 2f, world.sizeInCubes * Cube.CUBE_SCALE / 2f);
			Vector2 distFromCenter = new Vector2(center.X - Position.X, center.Y - Position.Z);

			if (distFromCenter.Length() > world.sizeInCubes * Cube.CUBE_SCALE / 2f)
			{
				distFromCenter = Vector2.Normalize(distFromCenter) * (world.sizeInCubes * Cube.CUBE_SCALE / 2f - 100f);
				//distFromCenter.X *= -1;
				//distFromCenter.Y *= -1;

				Position.X = center.X + distFromCenter.X;
				Position.Z = center.Y + distFromCenter.Y;
			}

			UpdateItemPickup();

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

								state = State.Hurt;

								inputLockupTimer = 0.25f;
								invulnTimer = 0.5f;
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
				Main.Debug = !Main.Debug;
			}

			if (Main.inputManager.JustPressed(Keys.E))
			{
				inventoryOpen = !inventoryOpen;
				Main.DrawCursor = inventoryOpen;
				Main.MouseControl = inventoryOpen;
				Mouse.SetPosition(Main.WindowResolution.X / 2, Main.WindowResolution.Y / 2);
			}

			if (Main.inputManager.JustPressed(Keys.D1))
			{
				inventoryInteractor.HighlightIndex = 0;
			}

			if (Main.inputManager.JustPressed(Keys.D2))
			{
				inventoryInteractor.HighlightIndex = 1;
			}

			if (Main.inputManager.JustPressed(Keys.D3))
			{
				inventoryInteractor.HighlightIndex = 2;
			}

			if (Main.inputManager.JustPressed(Keys.D4))
			{
				inventoryInteractor.HighlightIndex = 3;
			}

			if (Main.inputManager.JustPressed(Keys.D5))
			{
				inventoryInteractor.HighlightIndex = 4;
			}

			if (Main.inputManager.JustPressed(Keys.D6))
			{
				inventoryInteractor.HighlightIndex = 5;
			}

			if (Main.inputManager.JustPressed(Keys.D7))
			{
				inventoryInteractor.HighlightIndex = 6;
			}
			
			if (Main.inputManager.JustPressed(Keys.D8))
			{
				inventoryInteractor.HighlightIndex = 7;
			}

			lookAtResult = world.Raycast(-Main.camera.Position, -Main.camera.Position - Main.camera.Forward * INTERACT_DISTANCE,
			(Vector3 pos) =>
			{
				return world.GetChunkManager().IsInWorldBounds(pos) && world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air).Solid;
			});

			if (lookAtResult.hasHit)
			{
				if (world.GetChunkManager().IsInWorldBounds(lookAtResult.hit))
				{
					this.lookAtPos = CubePosition.FromWorldSpace(lookAtResult.hit);
					this.placeAtPos = CubePosition.FromWorldSpace(lookAtResult.hit + CubePosition.ToWorldSpaceV3(lookAtResult.normal));
				}
			}

			if (inventoryOpen)
			{
				inventoryInteractor.Update();
			}
			else
			{
				UpdateMouse();
			}

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

						if (world.GetChunkManager().IsInWorldBounds(chunkPos))
						{
							if (!world.GetChunkManager().IsChunkGenerated(chunkPos))
							{
								world.GetChunkManager().MarkGenerateDirty(chunkPos);
							}
						}
					}
				}
			}

			hitboxTimer -= (float)deltaTime;

			useTimer -= (float)deltaTime;
			itemUseCooldownTimer -= (float)deltaTime;

			alive += (float)deltaTime;

			lookAtColSine = (float)(Math.Sin(2 * Math.PI * ((alive % lookAtColTimeMax) / lookAtColTimeMax)) + 1f) / 2f;
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
			else
			{
				Vector3 actualMaxVel = MaxVelocity;

				bool movementPressed = false;
				bool running = false;

				if (inputLockupTimer <= 0 && !inventoryOpen)
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

					if (itemUseCooldownTimer <= 0 && (useTimer <= 0 ||
					Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton) ||
					Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton)))
					{
						if (Main.inputManager.IsPressed(A1r.Input.MouseInput.LeftButton))
						{
							if (inventory.Get(inventoryInteractor.HighlightIndex).item != null && inventory.Get(inventoryInteractor.HighlightIndex).item.LeftClick(this, inventory, inventoryInteractor.HighlightIndex, -Main.camera.Forward))
								PerformAction();
						}

						if (Main.inputManager.IsPressed(A1r.Input.MouseInput.RightButton))
						{
							if (inventory.Get(inventoryInteractor.HighlightIndex).item != null && inventory.Get(inventoryInteractor.HighlightIndex).item.RightClick(this, inventory, inventoryInteractor.HighlightIndex, -Main.camera.Forward))
								PerformAction();
						}
					}

					if (Main.inputManager.JustPressed(Keys.Q))
					{
						if (inventory.Get(inventoryInteractor.HighlightIndex).valid)
						{
							EntityItem ent = new Entities.EntityItem(Position, new ItemInstance(inventory.Get(inventoryInteractor.HighlightIndex), 1));
							world.EntityManager.Add(ent);
							ent.Velocity = -Main.camera.Forward * 100;

							inventory.Remove(inventoryInteractor.HighlightIndex, 1);
						}
					}

				}
				else
				{
					if (onGround)
					{
						Vector2 velXY = new Vector2(Velocity.X, Velocity.Z);

						if (velXY.Length() > 0)
						{
							velXY = Vector2.Normalize(velXY) * velXY.Length() * 0.65f;
						}

						Velocity = new Vector3(velXY.X, Velocity.Y, velXY.Y);
					}
					else
					{
						Vector2 velXY = new Vector2(Velocity.X, Velocity.Z);

						if (velXY.Length() > 0)
						{
							velXY = Vector2.Normalize(velXY) * velXY.Length() * 0.95f;
						}

						Velocity = new Vector3(velXY.X, Velocity.Y, velXY.Y);
					}
				}

				if (Main.inputManager.JustPressed(Keys.V))
				{
					Tree tree = new Tree(Position - new Vector3(0, Bounds.Size.Y, 0));
					world.EntityManager.Add(tree);
				}

				Velocity.Y += World.GRAVITY;
				if (Velocity.Y > actualMaxVel.Y)
					Velocity.Y = actualMaxVel.Y;
			}
		}

		private void UpdateItemPickup()
		{
			const float suckRadius = Cube.CUBE_SCALE * 3f;
			const float pickupRadius = Cube.CUBE_SCALE * 1.5f;

			var items = world.EntityManager.GetAll<EntityItem>();
			if (items != null)
			{
				foreach (var ent in items)
				{
					EntityItem item = ent as EntityItem;

					if (!inventory.CanAdd(item.Item.item))
						continue;

					Vector3 dir = Position - ent.Position;

					if (item.CanBePickedUp && dir.Length() < pickupRadius)
					{
						if (inventory.Add(item.Item))
							world.EntityManager.Remove(ent);
					}
					else if (item.CanBePickedUp && dir.Length() < suckRadius)
					{
						item.MoveTowards(Position);
					}
				}
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
			onGround = false;

			const float height = Cube.CUBE_SCALE * 2f;
			for (int i = 0; i < 4; i++)
			{
				Vector3 startPos = new Vector3(Position.X + offsetsDown[i].X, Position.Y - height, Position.Z + offsetsDown[i].Y);
				Vector3 dir = new Vector3(0, height, 0);
				var resultDown = world.RaycastVector(startPos, dir, height, (Vector3 pos) =>
				{
					return world.GetChunkManager().IsInWorldBounds(pos) && world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air).Solid;
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

			InWater = false;

			for (int i = 0; i < 4; i++)
			{
				Vector3 startPos = new Vector3(Position.X, Position.Y - height + Cube.CUBE_SCALE * 0.5f, Position.Z);
				ref Vector3 dir = ref directions[i];
				var resultSideBot = world.RaycastVector(startPos, dir, Cube.CUBE_SCALE * sideWidth, (Vector3 pos) =>
				{
					return world.GetChunkManager().IsInWorldBounds(pos) && world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air) != Main.Registry.CubeRegistry.Air;
				});

				if (resultSideBot.hasHit)
				{
					CubePosition pos = CubePosition.FromWorldSpace(resultSideBot.hit);

					if (world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air).GetType() == typeof(CubeWater))
						InWater = true;
					else
					{
						Vector3 offset = resultSideBot.hit - directions[i] * Cube.CUBE_SCALE * sideWidth;

						Position = new Vector3(offset.X, Position.Y, offset.Z);

						if (dir.X > 0 || dir.X < 0)
							Velocity.X = 0;
						if (dir.Z > 0 || dir.Z < 0)
							Velocity.Z = 0;
					}
				}
			}
		}

		private void UpdateMouse()
		{
			if (inventoryOpen)
				return;

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
			useTimer = ATTACK_TIME;
		}

		public void PerformAttack(float cooldownTimer)
		{
			Velocity.X = -Main.camera.Forward.X * 512f;

			if (onGround)
				Velocity.Y = -Main.camera.Forward.Y * 64f;
			else if (Velocity.Y > 20)
				Velocity.Y = 20;

			Velocity.Z = -Main.camera.Forward.Z * 512f;

			var hitboxes = world.HitboxManager.GetAll();

			DenseHitboxArray.Hitbox nearestHitbox = new DenseHitboxArray.Hitbox();
			float nearestDot = float.MinValue;

			foreach (var hitbox in hitboxes)
			{
				if (hitbox.index == this.hitbox || hitbox.group != 1)
					continue;

				Vector3 dir = hitbox.bounds.Center - Position;
				if (dir.Length() < PULL_RADIUS + 20)
				{
					float dot = Vector3.Dot(-Main.camera.Forward, Vector3.Normalize(dir));

					if (dot > nearestDot)
					{
						nearestDot = dot;
						nearestHitbox = hitbox;
					}
				}
			}

			if (nearestHitbox.active)
			{
				attackStateTargetPos = nearestHitbox.bounds.Center;
			}

			this.itemUseCooldownTimer = cooldownTimer;
			this.attackStateTimer = itemUseCooldownTimer;
			this.attackStateMoveTimer = 1f / Main.FIXED_FPS;

			state = State.Attack;
		}

		public void SpawnHitbox(int damage, float knockback = 1)
		{
			const float hitboxSize = Cube.CUBE_SCALE * 1.75f;

			if (hitbox != -1)
				world.HitboxManager.Remove(hitbox);

			damageDir = -Main.camera.Forward * (hitboxSize + 10f);

			Rectangle3D rect = new Rectangle3D(Position + damageDir - new Vector3(hitboxSize / 2), new Vector3(hitboxSize));
			this.hitboxSize = hitboxSize;

			hitbox = world.HitboxManager.Add(rect, -Main.camera.Forward, GROUP_PLAYER_SOURCE, damage, knockback);

			hitboxTimer = HITBOX_TIME;

			useTimer = ATTACK_TIME;
		}

		public void Draw(GraphicsDevice device)
		{
			if (inventory.Get(inventoryInteractor.HighlightIndex).item != null)
				inventory.Get(inventoryInteractor.HighlightIndex).item.Draw(device, this, -Main.camera.Forward);

			if (lookAtMesh == null)
			{
				lookAtMesh = MeshHelper.MakeCubeVertexPositionColor(device, Vector3.Zero, new Vector3(Cube.CUBE_SCALE), MeshHelper.CubeFace.ALL, Color.White, DrawHelper.WhitePixel);
			}

			if (lookAtResult.hasHit && world.GetChunkManager().IsInWorldBounds(lookAtResult.hit))
			{
				device.DepthStencilState = Main.genericDSS;
				device.RasterizerState = Main.wireframeRS;
				Main.BasicEffect.DiffuseColor = Color.Lerp(Color.Transparent, Color.Red, lookAtColSine).ToVector3();
				lookAtMesh.Draw(device, Main.BasicEffect, Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 2f)) *
					Matrix.CreateScale(1.126f) *
					Matrix.CreateTranslation(new Vector3(Cube.CUBE_SCALE / 2f)) * 
					Matrix.CreateTranslation(lookAtPos.InWorldSpace(null)));
				Main.BasicEffect.DiffuseColor = Color.White.ToVector3();

				device.DepthStencilState = Main.genericDSS;
				device.RasterizerState = Main.genericRS;
			}
		}

		public void DrawUI(SpriteBatch batch)
		{

			inventoryInteractor.DrawUnopened(batch);

			if (inventoryOpen)
				inventoryInteractor.DrawOpen(batch);
		}

		public Matrix GetHeldMatrix()
		{
			float percent = useTimer / ATTACK_TIME;

			if (useTimer <= 0)
				percent = 0;

			return Matrix.CreateTranslation(Vector3.Forward * Cube.CUBE_SCALE * 2 + Vector3.Down * Cube.CUBE_SCALE * 1.25f + Vector3.Right * Cube.CUBE_SCALE * 0.85f) *
				Matrix.CreateRotationX(-Main.camera.Rotation.X - MathHelper.ToRadians(35) * percent) *
					Matrix.CreateRotationY(-Main.camera.Rotation.Y - MathHelper.ToRadians(35) * percent) *
					Matrix.CreateRotationZ(-Main.camera.Rotation.Z) *
					Matrix.CreateTranslation(Position /*- Main.camera.Forward * Cube.CUBE_SCALE * 2 + Main.camera.Right * 8*/);
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
