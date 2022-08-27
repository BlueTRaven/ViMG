using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Items;
using ViMG.UIs;

namespace ViMG
{
	[Serializable]
	[EntityMeta(2, 0)]
	public class Player : Entity, IHitboxOwner
	{
		public const int GROUP_PLAYER_TAKE_SOURCE = 0;
		public const int GROUP_PLAYER_DEAL_SOURCE = 2;

		public const float INTERACT_DISTANCE = Cube.CUBE_SCALE * 4.5f;

		private enum State
		{
			Noclip,
			Normal,
			Attack,
			Hurt,
		}

		public Vector3 Velocity;

		private float moveSpeed = 8;
		public Vector3 MaxVelocity = new Vector3(64, 340, 64);
		public Vector3 MaxVelocityRunning = new Vector3(128, 340, 128);
		public float MaxFallVelocity;

		public float jumpVelocity = 256;

		private MouseState currentMS;
		private MouseState originalMS;

		private State state;

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

		public const int INVENTORY_ROWS = 4;
		public const int INVENTORY_COLUMNS = 8;

		private Inventory inventory;
		private Inventory craftInventory;
		private UIInventory currentUI;
		private UIInventoryPlayer uiPlayer;
		
		public Player()
		{
			AlwaysRender = true;
			//Position = new Vector3(world.sizeInCubes * Cube.CUBE_SCALE / 2f, world.sizeInCubes * Cube.CUBE_SCALE, world.sizeInCubes * Cube.CUBE_SCALE / 2f);

			//state = State.Noclip;
			Mouse.SetPosition(Main.WindowResolution.X / 2, Main.WindowResolution.Y / 2);
			originalMS = Mouse.GetState();

			craftInventory = new Inventory(8);
		}

		public void FirstCreated()
		{
			inventory = new Inventory(INVENTORY_ROWS * INVENTORY_COLUMNS);

			uiPlayer = new UIInventoryPlayer(this, inventory, craftInventory);
			currentUI = uiPlayer;

			inventory.Add(ItemPickaxe.CreatePickaxe(new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_tin"), 1, 1)));//new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_base"), 1, 1));
			inventory.Add(ItemSword.CreateSword(new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_tin"), 1, 1)));
		}

		public override void Update(double deltaTime)
		{
			world.ChunkLoadManager.UpdateLoadTarget(Position);

			if (Main.Debug)
				state = State.Noclip;
			else if (state == State.Noclip)
				state = State.Normal;

			if (!Main.Debug && world.GetChunkManager().IsInWorldBounds(Position) && (world.GetChunkManager().GetChunk(ChunkPosition.WorldSpaceChunk(Position)) == null || !world.GetChunkManager().GetChunk(ChunkPosition.WorldSpaceChunk(Position)).Initialized))
				return;

			if (hurtbox == -1)
				hurtbox = world.HitboxManager.Add(this, Bounds, Vector3.Zero, 0, -1, -1f);
			else if (state != State.Noclip)
				world.HitboxManager.Update(hurtbox, Bounds);

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

				UpdateMovement(deltaTime);

				Position += Velocity * (float)deltaTime;

				UpdateCollision();

				Vector3 dir = attackStateTargetPos - Position;
				if (dir.Length() < PUSH_RADIUS)
				{
					Position -= Vector3.Normalize(dir) * Math.Min(dir.Length(), 128);
				}

				if (attackStateTimer <= 0)
				{
					attackStateTargetPos = Vector3.Zero;
					state = State.Normal;
				}
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

			//float sine = ((float)Math.Sin(MathHelper.Pi * 2 * ((alive % 10f) / 10f)) + 1f) / 2f;

			/*float positionY = Position.Y;
			float start = 3772;
			float end = 3772 - 128;

			float factor = 1 - ((positionY - start) / (end - start));
			factor = Math.Clamp(factor, 0, 1);
			Main.CubeEffect.Parameters["AmbientStrength"].SetValue(factor);*/

			if (Main.inputManager.JustPressed(Keys.G))
			{
				Main.Debug = !Main.Debug;
			}

			if (Main.inputManager.JustPressed(Keys.F3))
			{
				Main.DebugChunks = !Main.DebugChunks;
			}

			if (Main.inputManager.JustPressed(Keys.E))
			{
				if (currentUI == uiPlayer)
				{
					if (uiPlayer.Opened)
						CloseUI();
					else OpenUI(uiPlayer);
				}
				else CloseUI();
			}

			if (Main.inputManager.JustPressed(Keys.D1))
			{
				uiPlayer.HighlightIndex = 0;
			}

			if (Main.inputManager.JustPressed(Keys.D2))
			{
				uiPlayer.HighlightIndex = 1;
			}

			if (Main.inputManager.JustPressed(Keys.D3))
			{
				uiPlayer.HighlightIndex = 2;
			}

			if (Main.inputManager.JustPressed(Keys.D4))
			{
				uiPlayer.HighlightIndex = 3;
			}

			if (Main.inputManager.JustPressed(Keys.D5))
			{
				uiPlayer.HighlightIndex = 4;
			}

			if (Main.inputManager.JustPressed(Keys.D6))
			{
				uiPlayer.HighlightIndex = 5;
			}

			if (Main.inputManager.JustPressed(Keys.D7))
			{
				uiPlayer.HighlightIndex = 6;
			}
			
			if (Main.inputManager.JustPressed(Keys.D8))
			{
				uiPlayer.HighlightIndex = 7;
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

			currentUI.Update();
			UpdateMouse();

			Main.camera.Position = -Position;

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
					//Position = Vector3.Zero;
					world.SetTimeOfDay(World.DAY_CYCLE_TIME * 0.8f);
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
				Vector2 velXY = new Vector2(Velocity.X, Velocity.Z);

				if (inputLockupTimer <= 0 && !uiPlayer.Opened)
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
						Velocity.Y = jumpVelocity;
						onGround = false;
					}

					Vector2 clampXY = new Vector2(actualMaxVel.X, actualMaxVel.Z);
					velXY = new Vector2(Velocity.X, Velocity.Z);

					if (velXY.Length() > clampXY.Length())
					{
						velXY.Normalize();
						velXY *= clampXY.Length();
					}

					Velocity = new Vector3(velXY.X, Velocity.Y, velXY.Y);

					if (currentUI == uiPlayer && !uiPlayer.Opened && itemUseCooldownTimer <= 0 && (useTimer <= 0 ||
					Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton) ||
					Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton)))
					{
						if (Main.inputManager.IsPressed(A1r.Input.MouseInput.LeftButton))
						{
							if (inventory.Get(uiPlayer.HighlightIndex).item != null && inventory.Get(uiPlayer.HighlightIndex).item.LeftClick(this, inventory, uiPlayer.HighlightIndex, -Main.camera.Forward, out itemUseCooldownTimer))
								PerformAction();
						}

						if (Main.inputManager.IsPressed(A1r.Input.MouseInput.RightButton))
						{
							var tracker = world.EntityManager.GetEntityTrackingPosition(lookAtPos);
							if (tracker.HasValue() && tracker.Get().OnInteract(this))
								PerformAction();
							else if (inventory.Get(uiPlayer.HighlightIndex).item != null && inventory.Get(uiPlayer.HighlightIndex).item.RightClick(this, inventory, uiPlayer.HighlightIndex, -Main.camera.Forward, out itemUseCooldownTimer))
								PerformAction();
						}
					}

					if (Main.inputManager.JustPressed(Keys.Q))
					{
						if (inventory.Get(uiPlayer.HighlightIndex).valid)
						{
							ThrowItem(inventory, uiPlayer.HighlightIndex, 1);
						}
					}
				}

				if (!movementPressed)
				{
					if (velXY.Length() > 0)
					{
						float scalar = 0.85f;

						if (!onGround)
							scalar = 0.95f;

						velXY = Vector2.Normalize(velXY) * velXY.Length() * scalar;
					}
				}

				Velocity = new Vector3(velXY.X, Velocity.Y, velXY.Y);

				if (Velocity.Y > 64 && Main.inputManager.JustReleased(Keys.Space))
					Velocity.Y = 64;

				Velocity.Y += World.GRAVITY;
				if (Velocity.Y > actualMaxVel.Y)
					Velocity.Y = actualMaxVel.Y;
			}

			if (Main.inputManager.JustPressed(Keys.V))
			{
				world.SetTimeOfDay(World.DAY_CYCLE_TIME * 0.75f);
				//world.EntityManager.Add(new Skeleton(Position));

				//OpenUI(new UIRecipeBook(Main.Registry.CubeRegistry.Get("furnace_t1") as CubeFurnace, new ItemInstance(Main.Registry.ItemRegistry.Get("iron_ingot"), 1, 1)));
				/*using (FileStream fs = new FileStream("./depth.png", FileMode.OpenOrCreate))
				{
					Main.DepthTarget.SaveAsPng(fs, Main.DepthTarget.Width, Main.DepthTarget.Height);
				}*/
			}
		}

		public void ThrowItem(Inventory inventory, int index, int num)
		{
			if (inventory.Get(uiPlayer.HighlightIndex).valid)
			{
				EntityItem ent = new EntityItem(Position, new ItemInstance(inventory.Get(index), num));
				world.EntityManager.Add(ent);
				ent.Velocity = -Main.camera.Forward * 100;

				inventory.Remove(index, num);
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

		private void UpdateCollision()
		{
			onGround = false;

			for (int x = -2; x <= 2; x++)
			{
				for (int y = -2; y <= 2; y++)
				{
					for (int z = -2; z <= 2; z++)
					{
						CubePosition pos = CubePosition.FromWorldSpace(Position) + new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace); //CubePosition.FromWorldSpace(Position);

						if (world.GetChunkManager().IsInWorldBounds(pos) && world.GetChunkManager().GetRaw(pos) != 0)
						{
							Rectangle3D cubeBounds = CubePosition.BoundsWorldSpace(pos);

							Vector3 checkPos = Position - new Vector3(0, Bounds.Size.Y - 16, 0);
							if (CollisionHelper.CheckCollision(cubeBounds, checkPos, 8, out Vector3 change))
							{
								Position = checkPos + new Vector3(0, Bounds.Size.Y - 16, 0) + change;

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
							else if (CollisionHelper.CheckCollision(cubeBounds, Position, 8f, out Vector3 change1))
							{
								Position += change1;

								if (change1.Y != 0)
									Velocity.Y = 0;
								else if (change1.X != 0)
									Velocity.X = 0;
								else if (change1.Z != 0)
									Velocity.Z = 0;
							}
						}
					}
				}
			}
		}

		private void UpdateMouse()
		{
			if (uiPlayer.Opened || currentUI != uiPlayer)
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
			//Velocity.X = -Main.camera.Forward.X * 512f;

			if (onGround)
				Velocity.Y = -Main.camera.Forward.Y * 64f;
			else if (Velocity.Y > 20)
				Velocity.Y = 20;

			//Velocity.Z = -Main.camera.Forward.Z * 512f;

			var hitboxes = world.HitboxManager.GetAll();

			HitboxManager.Hitbox nearestHitbox = new HitboxManager.Hitbox();
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

			hitbox = world.HitboxManager.Add(this, rect, -Main.camera.Forward, GROUP_PLAYER_DEAL_SOURCE, damage, knockback);

			hitboxTimer = HITBOX_TIME;

			useTimer = ATTACK_TIME;
		}

		public void OpenUI(UIInventory ui)
		{
			this.currentUI = ui;

			if (ui == uiPlayer)
				uiPlayer.Opened = true;

			Main.DrawCursor = true;
			Main.MouseControl = true;

			Mouse.SetPosition(Main.WindowResolution.X / 2, Main.WindowResolution.Y / 2);
		}

		public void CloseUI()
		{
			this.currentUI = uiPlayer;

			uiPlayer.Opened = false;

			Main.DrawCursor = false;
			Main.MouseControl = false;

			Mouse.SetPosition(Main.WindowResolution.X / 2, Main.WindowResolution.Y / 2);
		}

		public override void Draw(GraphicsDevice device)
		{
			//float sine = ((float)Math.Sin(MathHelper.Pi * 2 * ((alive % 10f) / 10f)) + 1f) / 2f;

			//Main.CubeEffect.Parameters["AmbientStrength"].SetValue(1f * sine);
			Main.LightManager.SetToEffect(Main.CubeEffect);

			if (inventory.Get(uiPlayer.HighlightIndex).item != null)
			{
				device.DepthStencilState = Main.nodepthDSS;
				inventory.Get(uiPlayer.HighlightIndex).item.DrawInHand(device, inventory.Get(uiPlayer.HighlightIndex), this, -Main.camera.Forward);
				device.DepthStencilState = Main.genericDSS;
			}

			if (lookAtMesh == null)
			{
				lookAtMesh = MeshHelper.MakeCubeVertexPositionColor(device, Vector3.Zero, new Vector3(Cube.CUBE_SCALE), MeshHelper.CubeFace.ALL, Color.White, DrawHelper.WhitePixel);
				lookAtMesh.Name = "Look At Mesh";
			}

			if (lookAtResult.hasHit && world.GetChunkManager().IsInWorldBounds(lookAtResult.hit))
			{
				device.DepthStencilState = Main.genericDSS;
				device.RasterizerState = Main.wireframeRS;
				
				//Main.BasicEffect.DiffuseColor = Color.Lerp(Color.Transparent, Color.Red, lookAtColSine).ToVector3();
				lookAtMesh.DrawDebugVertexPositionColor(device, Main.VertexPositionColorDebugEffect, Color.White, 
					Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 2f)) *
					Matrix.CreateScale(1.126f) *
					Matrix.CreateTranslation(new Vector3(Cube.CUBE_SCALE / 2f)) * 
					Matrix.CreateTranslation(lookAtPos.InWorldSpace(null)));
				//Main.BasicEffect.DiffuseColor = Color.White.ToVector3();

				device.DepthStencilState = Main.genericDSS;
				device.RasterizerState = Main.genericRS;
			}
		}

		public void DrawUI(SpriteBatch batch)
		{
			currentUI.Draw(batch);
		}

		public Inventory GetInventory()
		{
			return inventory;
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

		public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
		{
			if (invulnTimer <= 0)
			{
				if (us.group == GROUP_PLAYER_TAKE_SOURCE && other.group == Slime.GROUP_ENEMYHOSTILE_SOURCE)
				{
					Vector3 direction = Vector3.Normalize(Bounds.Center - other.bounds.Center);

					Velocity = new Vector3(direction.X * 128, 128, direction.Z * 128);

					state = State.Hurt;

					inputLockupTimer = 0.25f;
					invulnTimer = 4;
				}
			}
		}

		public override void OnSave(List<byte> saveBytes)
		{
			base.OnSave(saveBytes);

			SaveHelper.SaveCubePosition(saveBytes, CubePosition.FromWorldSpace(Position));
			SaveHelper.SaveVector3(saveBytes, Main.camera.Rotation);

			inventory.Save(saveBytes);
		}

		public override void OnLoad(byte[] loadBytes, in int version)
		{
			base.OnLoad(loadBytes, version);

			int index = 0;

			Position = SaveHelper.LoadCubePosition(loadBytes, ref index).InWorldSpace(null) + new Vector3(0, Cube.CUBE_SCALE, 0);

			Main.camera.Rotation = SaveHelper.LoadVector3(loadBytes, ref index);

			inventory = Inventory.Load(loadBytes, ref index);

			uiPlayer = new UIInventoryPlayer(this, inventory, craftInventory);
			currentUI = uiPlayer;
		}
	}
}
