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
	[EntityMeta(5, 0)]
	public class Player : Entity, IHitboxOwner
	{
		public struct AccumulatedStats
        {
			public int AdditionalHP;
			public int Defense;
			public float KnockbackResist;
			public float Speed;			//Adds to xz max velocity
			public float Acceleration;  //Adds to xz accel
			public float JumpSpeed;
			public int JumpNum;
			public float InvulnTime;
			public float UseSpeed;

			public static AccumulatedStats operator +(AccumulatedStats a, AccumulatedStats b)
            {
				return new AccumulatedStats()
				{
					AdditionalHP = a.AdditionalHP + b.AdditionalHP,
					Defense = a.Defense + b.Defense,
					KnockbackResist = a.KnockbackResist + b.KnockbackResist,
					Speed = a.Speed + b.Speed,         
					Acceleration = a.Acceleration + b.Acceleration, 
					JumpSpeed = a.JumpSpeed + b.JumpSpeed,
					JumpNum = a.JumpNum + b.JumpNum,
					InvulnTime = a.InvulnTime + b.InvulnTime,
					UseSpeed = a.UseSpeed + b.UseSpeed,
				};
            }
        }

		public const int GROUP_PLAYER_TAKE_SOURCE = 0;
		public const int GROUP_PLAYER_DEAL_SOURCE = 2;

		public const float INTERACT_DISTANCE = Cube.CUBE_SCALE * 4.5f;

		private enum State
		{
			Noclip,
			Normal,
			Swimming,
			Attack,
			Hurt,
		}

		public CubePosition SpawnPosition;
		private float loadedTimeOfDay = -1;

		public Vector3 Rotation;

		public Vector3 Velocity;

		private float moveSpeed = Cube.CUBE_SCALE * 0.4f;
		public Vector3 MaxVelocity = new Vector3(3.2f, 17, 3.2f) * Cube.CUBE_SCALE;
		public Vector3 MaxVelocityRunning = new Vector3(6.4f, 17, 6.4f) * Cube.CUBE_SCALE;
		public Vector3 MaxVelocitySwimming = new Vector3(2.8f) * Cube.CUBE_SCALE;
		public Vector3 MaxVelocitySwimmingFast = new Vector3(5.6f) * Cube.CUBE_SCALE;
		public float MaxFallVelocity;
		private float fallStartY;   //the upper-most point of the current jump. If the player hits something > FALL_HEIGHT_FATAL, they will die.
		private const float FALL_HEIGHT_DAMAGE_START = Cube.CUBE_SCALE * 5;
		private const float FALL_HEIGHT_FATAL = Cube.CUBE_SCALE * 18;

		public float jumpSpeed = 10f * Cube.CUBE_SCALE;

		private MouseState currentMS;
		private MouseState originalMS;

		private State state;

		private bool onGround;
		private bool inWater;
		private bool headUnderWater;

		private Rectangle3D Bounds => new Rectangle3D(Position + new Vector3(-Cube.CUBE_SCALE * 0.85f / 2f, -Cube.CUBE_SCALE * 2f, -Cube.CUBE_SCALE * 0.85f / 2f),
			new Vector3(Cube.CUBE_SCALE * 0.85f, Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 0.85f));
		private int hurtbox = -1;
		private float invulnTimer;
		private float inputLockupTimer;
		private float damageAnimTimer;
		private const float DAMAGE_ANIM_TIME = 15f / 60f;

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
		//Is currently looking at a cube or not
		public bool IsLooking;
		//position that the player is currently looking at (if any), in cube space.
		//Will be the position of the last looked at object if nothing is currently looked at.
		public CubePosition LookAtPos;
		//likewise, this is the position the player will place a cube if they right clicked the LookAtPos
		//with a cube item in hand.
		public CubePosition PlaceAtPos;	
		private float alive = 0;

		private const float PULL_RADIUS = 3.25f * Cube.CUBE_SCALE;
		private const float NEUTRAL_RADIUS = 2.5f * Cube.CUBE_SCALE;
		private const float PUSH_RADIUS = 1.75f * Cube.CUBE_SCALE;
		private Vector3 attackStateTargetPos;

		private SimpleMesh<VertexCube, int> lookAtMesh;
		private SimpleMesh<VertexCube, int> itemMesh;
		private (VertexBuffer VBO, IndexBuffer IBO) testMesh;

		public const int INVENTORY_ROWS = 4;
		public const int INVENTORY_COLUMNS = 8;

		private Inventory inventory;
		private Inventory craftInventory;
		private Inventory accessoryInventory;
		private UIInventory currentUI;
		private UIInventoryPlayer uiPlayer;

		public int health;
		public int maxHealth = 20;
		private AccumulatedStats stats;

		private bool hasMoved;
		private bool hasRotated;
		
		public Player()
		{
			AlwaysRender = true;
			//Position = new Vector3(world.sizeInCubes * Cube.CUBE_SCALE / 2f, world.sizeInCubes * Cube.CUBE_SCALE, world.sizeInCubes * Cube.CUBE_SCALE / 2f);

			//state = State.Noclip;
			Options.CenterMouse();
			originalMS = Mouse.GetState();
			Rotation = Main.camera.Rotation;

			accessoryInventory = new Inventory(3);  //TODO deadPlayer.GetAccessoryInventory(); - Get rid of this
			craftInventory = new Inventory(8);

			health = maxHealth;
		}

		//Creates a new player from a dead player.
		public Player(Player deadPlayer)
        {
			invulnTimer = 6f;	//6 seconds of invuln after respawning

			inventory = deadPlayer.GetInventory();
			accessoryInventory = new Inventory(3);	//TODO deadPlayer.GetAccessoryInventory();
			SpawnPosition = deadPlayer.SpawnPosition;
			Position = deadPlayer.SpawnPosition.InWorldSpace(null);

			AlwaysRender = true;

			Options.CenterMouse();
			originalMS = Mouse.GetState();
			Rotation = Main.camera.Rotation;

			craftInventory = new Inventory(8);

			uiPlayer = new UIInventoryPlayer(this, inventory, craftInventory, accessoryInventory);
			currentUI = uiPlayer;

			health = maxHealth / 4;
		}

		public void FirstCreated()
		{
			inventory = new Inventory(INVENTORY_ROWS * INVENTORY_COLUMNS);
			accessoryInventory = new Inventory(3);

			uiPlayer = new UIInventoryPlayer(this, inventory, craftInventory, accessoryInventory);
			currentUI = uiPlayer;

			inventory.Add(ItemPickaxe.CreatePickaxe(new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_tin"), 1, 1)));//new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_base"), 1, 1));
			inventory.Add(ItemSword.CreateSword(new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_tin"), 1, 1)));
		}

        public override void Initialize(World world)
        {
            base.Initialize(world);

			//If we loaded the time of day, set the world's time of day to it.
			if (loadedTimeOfDay > 0)
			{
				world.SetTime(loadedTimeOfDay);
				loadedTimeOfDay = -1;
			}

			//SpawnPosition got corrupted or something or is a version that doesn't have it
			if (SpawnPosition == new CubePosition())
			{
				SpawnPosition = world.ChunkManager.GetFirstSolidDown(new CubePosition(world.sizeInCubes / 2, world.sizeInCubes, world.sizeInCubes / 2)).GetOrDefault(new CubePosition());
			}
		}

        public override void OnDelete()
        {
            base.OnDelete();

			for (int i = 0; i < craftInventory.NumSlots; i++)
			{
				if (craftInventory.Get(i).valid)
				{
					EntityItem ent = new EntityItem(Position, craftInventory.Get(i));
					ent.Velocity = new Vector3(Main.random.NextFloat(-5 * Cube.CUBE_SCALE, 5 * Cube.CUBE_SCALE),
						6.4f * Cube.CUBE_SCALE, Main.random.NextFloat(-5 * Cube.CUBE_SCALE, 5 * Cube.CUBE_SCALE));
					world.EntityManager.Add(ent);
				}
			}

			if (hitbox != -1)
				world.HitboxManager.Remove(hitbox);
			hitbox = -1;

			//TODO death screen and stuff
			Player p = new Player(this);
			world.EntityManager.Add(p);
			world.player = p;
		}

        public override void Update(double deltaTime)
		{
			hasMoved = false;
			hasRotated = false;

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

			UpdateStats();

			if (state == State.Noclip)
			{
				UpdateMovement(deltaTime);
			}
			else if (state == State.Normal)
			{
				invulnTimer -= (float)deltaTime;

				if (inWater)
				{
					UpdateMovementWater(deltaTime);
				}
				else
				{
					UpdateMovement(deltaTime);
				}
				
				UpdateCollision(deltaTime);
				
				Position += Velocity * (float)deltaTime;
			}
			else if (state == State.Attack)
			{
				invulnTimer -= (float)deltaTime;

				attackStateTimer -= (float)deltaTime;
				attackStateMoveTimer -= (float)deltaTime;

				UpdateMovement(deltaTime);

				UpdateCollision(deltaTime);

				Position += Velocity * (float)deltaTime;

				Vector3 dir = attackStateTargetPos - Position;
				if (dir.Length() < PUSH_RADIUS)
				{
					Position -= Vector3.Normalize(dir) * Math.Min(dir.Length(), 6.4f * Cube.CUBE_SCALE);
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

			damageAnimTimer -= (float)deltaTime;

			float worldRadius = world.sizeInCubes / 2f * Cube.CUBE_SCALE;
			Vector2 center = new Vector2(worldRadius, worldRadius);
			Vector2 distFromCenter = new Vector2(center.X - Position.X, center.Y - Position.Z);

			if (distFromCenter.Length() > worldRadius)
			{
				distFromCenter = Vector2.Normalize(distFromCenter) * (worldRadius - (Cube.CUBE_SCALE * 5));

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

			int oldHighlight = uiPlayer.HighlightIndex;
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

			if (oldHighlight != uiPlayer.HighlightIndex)
            {
				if (inventory.Get(oldHighlight).valid)
				{
					inventory.Get(oldHighlight).item.EndHold(this, inventory, uiPlayer.HighlightIndex);

					if (inventory.Get(uiPlayer.HighlightIndex).valid)
						inventory.Get(oldHighlight).item.StartHold(this, inventory, uiPlayer.HighlightIndex);
				}
			}

			if (inventory.Get(uiPlayer.HighlightIndex).valid)
				inventory.Get(uiPlayer.HighlightIndex).item.Hold(this, inventory, uiPlayer.HighlightIndex);

			lookAtResult = world.Raycast(Position, Position - Main.camera.Forward * INTERACT_DISTANCE,
			(Vector3 pos) =>
			{
				//return true;
				return world.GetChunkManager().IsInWorldBounds(pos) && world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air).Touchable;
			});

			IsLooking = false;
			if (lookAtResult.hasHit)
			{
				if (world.GetChunkManager().IsInWorldBounds(lookAtResult.hit))
				{
					var c = world.GetChunkManager().GetCube(lookAtResult.hit);
					IsLooking = true;
					this.LookAtPos = CubePosition.FromWorldSpace(lookAtResult.hit);
					this.PlaceAtPos = CubePosition.FromWorldSpace(lookAtResult.hit + CubePosition.ToWorldSpaceV3(lookAtResult.normal));
				}
			}

			currentUI.Update();
			UpdateMouse();

			hitboxTimer -= (float)deltaTime;

			useTimer -= (float)deltaTime;
			itemUseCooldownTimer -= (float)deltaTime;

			alive += (float)deltaTime;
		}

		private void UpdateStats()
		{
			AccumulatedStats accumulatedStats = new AccumulatedStats();

			for (int i = 0; i < accessoryInventory.NumSlots; i++)
			{
				ItemInstance item = accessoryInventory.Get(i);

				if (item.valid)
				{
					item.item.AccumulateStats(this, accessoryInventory, i, ref accumulatedStats);
				}
			}

			stats = accumulatedStats;
		}

		private void UpdateMovementWater(double deltaTime)
		{
			Vector3 actualMaxVel = MaxVelocitySwimming;

			bool movementPressed = false;
			bool jumpHeld = false;
			bool swimmingFast = false;

			if (inputLockupTimer <= 0 && !uiPlayer.Opened)
			{
				if (Main.inputManager.IsHeld(Keys.LeftShift))
					swimmingFast = true;

				if (swimmingFast)
					actualMaxVel = MaxVelocitySwimmingFast;

				actualMaxVel += new Vector3(stats.Speed, 0, stats.Speed);

				float actualAcceleration = moveSpeed + stats.Acceleration;

				if (Main.inputManager.IsPressed(Keys.W))
				{
					Velocity -= Vector3.Normalize(Main.camera.Forward) * actualAcceleration;
					movementPressed = true;
				}
				if (Main.inputManager.IsPressed(Keys.S))
				{
					Velocity += Vector3.Normalize(Main.camera.Forward) * actualAcceleration;
					movementPressed = true;
				}
				if (Main.inputManager.IsPressed(Keys.A))
				{
					Velocity -= Vector3.Normalize(Main.camera.Right) * actualAcceleration;
					movementPressed = true;
				}
				if (Main.inputManager.IsPressed(Keys.D))
				{
					Velocity += Vector3.Normalize(Main.camera.Right) * actualAcceleration;
					movementPressed = true;
				}
				if (Main.inputManager.IsPressed(Keys.Space))
				{
					Velocity.Y += moveSpeed;
					movementPressed = true;
					jumpHeld = true;
				}

				if (Velocity.Length() > actualMaxVel.Length())
				{
					Velocity.Normalize();
					Velocity *= actualMaxVel.Length();
				}

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
						var tracker = world.EntityManager.GetEntityTrackingPosition(LookAtPos);
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
				if (Velocity.Length() > 0)
				{
					float decel = Cube.CUBE_SCALE / 8f;

					Velocity = Vector3.Normalize(Velocity) * MathF.Max(Velocity.Length() - decel, 0);
				}
			}

			if (Velocity.Length() > float.Epsilon)
				hasMoved = true;

			if (Velocity.Y > Cube.CUBE_SCALE * 3.2f && Main.inputManager.JustReleased(Keys.Space))
				Velocity.Y = Cube.CUBE_SCALE * 3.2f;

			if (!jumpHeld)
				Velocity.Y += World.GRAVITY;

			if (!onGround && Velocity.Y > 0)
            {
				fallStartY = Position.Y;
            }

			if (Velocity.Y < -actualMaxVel.Y)
				Velocity.Y = -actualMaxVel.Y;
			if (Velocity.Y > actualMaxVel.Y)
				Velocity.Y = actualMaxVel.Y;
		}

		private void UpdateMovement(double deltaTime)
		{
			if (state == State.Noclip)
			{
				const float MIN_CAM_SPEED = Cube.CUBE_SCALE;
				const float MAX_CAM_SPEED = MIN_CAM_SPEED * 2;

				float moveSpeed = MIN_CAM_SPEED;

				if (Main.inputManager.IsHeld(Keys.LeftShift))
					moveSpeed = MAX_CAM_SPEED;

				Vector3 oldPos = Position;

				if (Main.inputManager.IsPressed(Keys.W))
					Position -= Vector3.Normalize(Main.camera.Forward) * moveSpeed;
				if (Main.inputManager.IsPressed(Keys.S))
					Position += Vector3.Normalize(Main.camera.Forward) * moveSpeed;
				if (Main.inputManager.IsPressed(Keys.A))
					Position -= Vector3.Normalize(Main.camera.Right) * moveSpeed;
				if (Main.inputManager.IsPressed(Keys.D))
					Position += Vector3.Normalize(Main.camera.Right) * moveSpeed;

				if (Position != oldPos)
					hasMoved = true;

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
						var tracker = world.EntityManager.GetEntityTrackingPosition(LookAtPos);
						if (tracker.HasValue() && tracker.Get().OnInteract(this))
							PerformAction();
						else if (inventory.Get(uiPlayer.HighlightIndex).item != null && inventory.Get(uiPlayer.HighlightIndex).item.RightClick(this, inventory, uiPlayer.HighlightIndex, -Main.camera.Forward, out itemUseCooldownTimer))
							PerformAction();
					}
				}

				fallStartY = Position.Y;	//so we don't immediately die sometimes
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

					actualMaxVel += new Vector3(stats.Speed, 0, stats.Speed);

					float actualAcceleration = moveSpeed + stats.Acceleration;

					if (Main.inputManager.IsPressed(Keys.W))
					{
						Velocity -= Vector3.Normalize(Main.camera.ForwardYawOnly) * actualAcceleration;
						movementPressed = true;
					}
					if (Main.inputManager.IsPressed(Keys.S))
					{
						Velocity += Vector3.Normalize(Main.camera.ForwardYawOnly) * actualAcceleration;
						movementPressed = true;
					}
					if (Main.inputManager.IsPressed(Keys.A))
					{
						Velocity -= Vector3.Normalize(Main.camera.Right) * actualAcceleration;
						movementPressed = true;
					}
					if (Main.inputManager.IsPressed(Keys.D))
					{
						Velocity += Vector3.Normalize(Main.camera.Right) * actualAcceleration;
						movementPressed = true;
					}
					if (onGround && Main.inputManager.JustPressed(Keys.Space))
					{
						Velocity.Y = jumpSpeed + stats.JumpSpeed;
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
							var tracker = world.EntityManager.GetEntityTrackingPosition(LookAtPos);
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
						float decel = Cube.CUBE_SCALE / 2f;

						if (!onGround)
							decel = Cube.CUBE_SCALE / 8f;

						velXY = Vector2.Normalize(velXY) * MathF.Max(velXY.Length() - decel, 0);
					}
				}

				Velocity = new Vector3(velXY.X, Velocity.Y, velXY.Y);

				if (Velocity.Length() > float.Epsilon)
					hasMoved = true;

				if (Velocity.Y > Cube.CUBE_SCALE * 3.2f && Main.inputManager.JustReleased(Keys.Space))
					Velocity.Y = Cube.CUBE_SCALE * 3.2f;

				Velocity.Y += World.GRAVITY;
				if (Velocity.Y < -actualMaxVel.Y)
					Velocity.Y = -actualMaxVel.Y;
				if (Velocity.Y > actualMaxVel.Y)
					Velocity.Y = actualMaxVel.Y;
			}

			if (Main.inputManager.JustPressed(Keys.V))
			{
				world.AddTime(World.DAY_CYCLE_TIME * 0.25f);
				//world.EntityManager.Add(new CaveSalamander(Position - Main.camera.Forward * Cube.CUBE_SCALE * 4));
			}
		}

		public void ThrowItem(Inventory inventory, int index, int num)
		{
			if (inventory.Get(uiPlayer.HighlightIndex).valid)
			{
				ItemInstance thrownInstance = new ItemInstance(inventory.Get(index), num);
				EntityItem ent = new EntityItem(Position, thrownInstance);
				world.EntityManager.Add(ent);
				ent.Velocity = -Main.camera.Forward * Cube.CUBE_SCALE * 5;

				inventory.Remove(index, num);

				if (!inventory.Get(index).valid || inventory.Get(index).num == 0)
					thrownInstance.item.EndHold(this, inventory, index);
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
						if (inventory.Add(item.Item, out int index))
						{
							world.EntityManager.Remove(ent);
							item.Item.item.StartHold(this, inventory, index);
						}
					}
					else if (item.CanBePickedUp && dir.Length() < suckRadius)
					{
						item.MoveTowards(Position);
					}
				}
			}
		}

		private void UpdateCollision(double deltaTime)
		{
			inWater = false;
			onGround = false;
			headUnderWater = false;

			const float RADIUS = Cube.CUBE_SCALE * 0.4f;

			const float LOWER_OFFSET = Cube.CUBE_SCALE * 0.75f;

			Vector3 realVelocity = Velocity * (float)deltaTime;

			CubePosition near = CubePosition.FromWorldSpace(Bounds.Position + (realVelocity + realVelocity * RADIUS));
			CubePosition far = CubePosition.FromWorldSpace(Bounds.FarPosition + (realVelocity + realVelocity * RADIUS));

			if (far.X < near.X)
            {
				var temp = far.X;
				far.X = near.X;
				near.X = temp;
            }

			if (far.Y < near.Y)
			{
				var temp = far.Y;
				far.Y = near.Y;
				near.Y = temp;
			}

			if (far.Z < near.Z)
			{
				var temp = far.Z;
				far.Z = near.Z;
				near.Z = temp;
			}

			for (int x = near.X; x <= far.X; x++)
			{
				for (int y = near.Y; y <= far.Y; y++)
				{
					for (int z = near.Z; z <= far.Z; z++)
					{
						CubePosition pos = new CubePosition(x, y, z);

						Cube cube = world.GetChunkManager().GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air);
						if (world.GetChunkManager().IsInWorldBounds(pos) && cube.Id != 0 && 
							(cube.Collision == Cube.CollisionValue.Collidable || cube.Collision == Cube.CollisionValue.LiquidWater))
						{
							Rectangle3D cubeBounds = CubePosition.BoundsWorldSpace(pos);

							for (int i = 0; i < 4; i++)
							{
								Vector3 segmentVelocity = (Velocity / 4f * i) * (float)deltaTime;

								Vector3 lowerCheckPos = Position - new Vector3(0, Bounds.Size.Y - LOWER_OFFSET, 0) + segmentVelocity;
								Vector3 upperCheckPos = Position + segmentVelocity;

								bool collided = false;

								if (cube.Collision == Cube.CollisionValue.LiquidWater)
								{
									if (CollisionHelper.CheckCollision(cubeBounds, lowerCheckPos, RADIUS, out Vector3 lowerChange))
									{
										inWater = true;
										collided = true;
									}

									if (CollisionHelper.CheckCollision(cubeBounds, upperCheckPos, RADIUS, out Vector3 upperChange))
									{
										inWater = true;
										headUnderWater = true;
										collided = true;
									}
								}
								else
								{
									if (CollisionHelper.CheckCollision(cubeBounds, lowerCheckPos, RADIUS, out Vector3 lowerChange))
									{
										Position = lowerCheckPos + new Vector3(0, Bounds.Size.Y - LOWER_OFFSET, 0) + lowerChange;

										if (lowerChange.Y > 0 && Velocity.Y <= 0)
										{
											Velocity.Y = 0;
											onGround = true;

											float fallDistance = fallStartY - Position.Y;
											if (fallDistance > FALL_HEIGHT_FATAL)
												Kill();
											else
											{
												if (fallDistance > FALL_HEIGHT_DAMAGE_START)
												{
													float t = (fallDistance - FALL_HEIGHT_DAMAGE_START) / (FALL_HEIGHT_FATAL - FALL_HEIGHT_DAMAGE_START);

													int damage = (int)((float)maxHealth * t);

													Damage(damage);
												}

												fallStartY = Position.Y;
											}
										}
										else if (lowerChange.Y < 0)
											Velocity.Y = 0;
										else if (lowerChange.X != 0)
											Velocity.X = 0;
										else if (lowerChange.Z != 0)
											Velocity.Z = 0;

										collided = true;
									}
									else if (CollisionHelper.CheckCollision(cubeBounds, upperCheckPos, RADIUS, out Vector3 upperChange))
									{
										Position = upperCheckPos + upperChange;

										if (upperChange.Y != 0)
											Velocity.Y = 0;
										else if (upperChange.X != 0)
											Velocity.X = 0;
										else if (upperChange.Z != 0)
											Velocity.Z = 0;

										collided = true;
									}
								}

								if (collided)
									break;
							}
						}
					}
				}
			}
		}

		private void UpdateMouse()
		{
			if (hasMoved)
				Main.camera.Position = Position;

			if (uiPlayer.Opened || currentUI != uiPlayer)
				return;

			currentMS = Mouse.GetState();

			if (currentMS != originalMS)
			{
				float scalar = 0.25f;

				Vector3 camRotation = Rotation;

				Vector2 delta = new Vector2(originalMS.X, originalMS.Y) - new Vector2(currentMS.X, currentMS.Y);

				if (delta.Length() > float.Epsilon)
				{
					hasRotated = true;

					camRotation.Y -= MathHelper.ToRadians(delta.X) * scalar;
					camRotation.X -= MathHelper.ToRadians(delta.Y) * scalar;

					if (camRotation.X > MathHelper.ToRadians(89))
						camRotation.X = MathHelper.ToRadians(89);
					else if (camRotation.X < -MathHelper.ToRadians(89))
						camRotation.X = -MathHelper.ToRadians(89);

					Rotation = camRotation;

					Main.camera.Rotation = Rotation;
				}
			}
		}

		public void PerformAction()
		{
			useTimer = state == State.Noclip ? 0.05f : ATTACK_TIME;
		}

		public void PerformAttack(float cooldownTimer)
		{
			//Velocity.X = -Main.camera.Forward.X * 512f;

			if (onGround)
				Velocity.Y = -Main.camera.Forward.Y * 3.2f * Cube.CUBE_SCALE;
			else if (Velocity.Y > Cube.CUBE_SCALE)
				Velocity.Y = Cube.CUBE_SCALE;

			//Velocity.Z = -Main.camera.Forward.Z * 512f;

			var hitboxes = world.HitboxManager.GetAll();

			HitboxManager.Hitbox nearestHitbox = new HitboxManager.Hitbox();
			float nearestDot = float.MinValue;

			foreach (var hitbox in hitboxes)
			{
				if (hitbox.index == this.hitbox || hitbox.group != 1)
					continue;

				Vector3 dir = hitbox.bounds.Center - Position;
				if (dir.Length() < PULL_RADIUS + Cube.CUBE_SCALE)
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

			damageDir = -Main.camera.Forward * (hitboxSize + 0.5f * Cube.CUBE_SCALE);

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

			Options.CenterMouse();
		}

		public void CloseUI()
		{
			this.currentUI = uiPlayer;

			uiPlayer.Opened = false;

			Main.DrawCursor = false;
			Main.MouseControl = false;

			Options.CenterMouse();
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			/*if (testMesh.VBO == null)
            {
				List<VertexCube> sunVertices = new List<VertexCube>();
				List<int> sunIndices = new List<int>();

				sunIndices.Add(0);
				sunIndices.Add(1);
				sunIndices.Add(3);
				sunIndices.Add(1);
				sunIndices.Add(2);
				sunIndices.Add(3);

				sunIndices.Add(3);
				sunIndices.Add(1);
				sunIndices.Add(0);
				sunIndices.Add(3);
				sunIndices.Add(2);
				sunIndices.Add(1);

				const float SUN_VERT_DIST = Cube.CUBE_SCALE * 6;
				sunVertices.Add(new VertexCube(new Vector3(-SUN_VERT_DIST, -SUN_VERT_DIST, 0), Color.Yellow, new Vector2(0, 1), new Vector3(0, 0, -1)));
				sunVertices.Add(new VertexCube(new Vector3(-SUN_VERT_DIST, SUN_VERT_DIST, 0), Color.Yellow, new Vector2(0, 0), new Vector3(0, 0, -1)));
				sunVertices.Add(new VertexCube(new Vector3(SUN_VERT_DIST, SUN_VERT_DIST, 0), Color.Yellow, new Vector2(1, 0), new Vector3(0, 0, -1)));
				sunVertices.Add(new VertexCube(new Vector3(SUN_VERT_DIST, -SUN_VERT_DIST, 0), Color.Yellow, new Vector2(1, 1), new Vector3(0, 0, -1)));

				testMesh = MeshHelper.MakeSimplerMesh(device, sunVertices, sunIndices);
            }
            else
            {
				float worldRadius = world.sizeInCubes / 2f * Cube.CUBE_SCALE;
				Vector2 worldCenter = new Vector2(worldRadius, worldRadius);
				Vector2 dirWorldCenter = new Vector2(worldCenter.X - Position.X, worldCenter.Y - Position.Z);
				float dist = dirWorldCenter.Length();

				const float MIN_DIST = Cube.CUBE_SCALE * 180;
				const float MAX_DIST = Cube.CUBE_SCALE * 224;

				//TODO: if dist > 232, do the thing...

				if (dist > Cube.CUBE_SCALE * 180)
				{
					Vector3 tpos = Position - Main.camera.ForwardYawOnly * Cube.CUBE_SCALE * 32;

					float alpha = (dist - MIN_DIST) / (MAX_DIST - MIN_DIST);

					Main.Renderer.DrawsTransparentPass.Add(new Rendering.RendererDeferred.TransparentDraw((int)(Cube.CUBE_SCALE * 40),
						Matrix.CreateRotationY(-Main.camera.Rotation.Y) * Matrix.CreateTranslation(tpos), 
						Main.assetsManager.GetAsset<Texture2D>("leviathan"), DrawHelper.WhitePixel, testMesh.VBO, testMesh.IBO, new RectangleF(0, 0, 64, 64), Color.White * alpha));
				}
			}*/

			//float sine = ((float)Math.Sin(MathHelper.Pi * 2 * ((alive % 10f) / 10f)) + 1f) / 2f;

			//Main.CubeEffect.Parameters["AmbientStrength"].SetValue(1f * sine);

			if (inventory.Get(uiPlayer.HighlightIndex).item != null)
			{
				inventory.Get(uiPlayer.HighlightIndex).item.DrawInHand(device, inventory.Get(uiPlayer.HighlightIndex), this, -Main.camera.Forward);
			}

			if (lookAtMesh == null)
			{
				lookAtMesh = MeshHelper.MakeCubeVertexPositionColorTextureNormal(device, Vector3.Zero, new Vector3(Cube.CUBE_SCALE), MeshHelper.CubeFace.ALL, Color.White, DrawHelper.WhitePixel);
				//lookAtMesh = MeshHelper.MakeCubeVertexPositionColor(device, Vector3.Zero, new Vector3(Cube.CUBE_SCALE), MeshHelper.CubeFace.ALL, Color.White, DrawHelper.WhitePixel);
				lookAtMesh.Name = "Look At Mesh";
			}

			if (lookAtResult.hasHit && world.GetChunkManager().IsInWorldBounds(lookAtResult.hit))
			{
				float s = MathF.Sin(MathF.PI * 2f * (alive % 2f)) * 0.5f + 0.5f;
				Color color = Color.Lerp(Color.White, Color.Black, s);

				Main.Renderer.DrawsTransparentPass.Add(new Rendering.RendererDeferred.TransparentDraw((int)lookAtResult.end.Length(),
					Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 2f)) *
					Matrix.CreateScale(1.126f) *
					Matrix.CreateTranslation(new Vector3(Cube.CUBE_SCALE / 2f)) *
					Matrix.CreateTranslation(LookAtPos.InWorldSpace(null)), 
					Main.assetsManager.GetAsset<Texture2D>("cubes_textures"), DrawHelper.BlackPixel,
					lookAtMesh.VBO, lookAtMesh.IBO, new RectangleF(0, 1008, 16, 16), color));
				/*device.DepthStencilState = Main.genericDSS;
				device.RasterizerState = Main.wireframeRS;
				
				//Main.BasicEffect.DiffuseColor = Color.Lerp(Color.Transparent, Color.Red, lookAtColSine).ToVector3();
				lookAtMesh.DrawDebugVertexPositionColor(device, Main.VertexPositionColorDebugEffect, Color.White, 
					Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 2f)) *
					Matrix.CreateScale(1.126f) *
					Matrix.CreateTranslation(new Vector3(Cube.CUBE_SCALE / 2f)) * 
					Matrix.CreateTranslation(LookAtPos.InWorldSpace(null)));
				//Main.BasicEffect.DiffuseColor = Color.White.ToVector3();

				device.DepthStencilState = Main.genericDSS;
				device.RasterizerState = Main.genericRS;*/
			}
		}

		public void DrawUI(SpriteBatch batch)
		{
			if (!Main.inputManager.IsHeld(Keys.F5))
			{
				currentUI.Draw(batch);

				if (alive < 0.5f)
				{
					float t = 1 - (alive / 0.5f);

					batch.DrawRectangle(new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y), Color.Black * t);
				}

				if (damageAnimTimer >= 0)
				{
					float t = damageAnimTimer / DAMAGE_ANIM_TIME;

					batch.DrawRectangle(new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y), Color.DarkRed * t);
				}

				if (headUnderWater)
				{
					batch.DrawRectangle(new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y), Color.Blue * 0.5f);
				}
			}
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

					Velocity = new Vector3(direction.X * 6.4f, 6.4f, direction.Z * 6.4f);

					state = State.Hurt;

					Damage(DamageCalculation(other));

					inputLockupTimer = 0.25f;
					invulnTimer = 4;
				}
			}
		}

		public void Kill()
        {
			//health = 0;
			world.EntityManager.Remove(this);
        }

		private void Damage(int amt)
        {
			damageAnimTimer = DAMAGE_ANIM_TIME;

			health -= amt;

			if (health <= 0)
			{
				world.EntityManager.Remove(this);
			}
		}

		private int DamageCalculation(HitboxManager.Hitbox hitbox)
        {
			float defenseCalc = (float)stats.Defense * 0.5f;

			float damage = (float)hitbox.damage - defenseCalc;

			if ((int)damage <= 0)
			{
				//If we have enough defense, negate damage entirely. Otherwise, max is 1.
				//Player must have at least 10 defense before this negation can be applied.
				if (stats.Defense > 10 && stats.Defense > hitbox.damage * 3)
					damage = 0;
				else damage = 1;
			}

			return (int)damage;
        }

		public void Heal(int amt)
        {
			health += amt;

			if (health > maxHealth + stats.AdditionalHP)
				health = maxHealth + stats.AdditionalHP;
        }

		public override void OnSave(List<byte> saveBytes)
		{
			base.OnSave(saveBytes);

			SaveHelper.SaveCubePosition(saveBytes, CubePosition.FromWorldSpace(Position));
			SaveHelper.SaveVector3(saveBytes, Main.camera.Rotation);

			SaveHelper.SaveInt32(saveBytes, health);
			SaveHelper.SaveInt32(saveBytes, maxHealth);

			inventory.Save(saveBytes);

			SaveHelper.SaveFloat32(saveBytes, world.GetTime());
			SaveHelper.SaveCubePosition(saveBytes, SpawnPosition);
		}

		public override void OnLoad(byte[] loadBytes, in int version)
		{
			base.OnLoad(loadBytes, version);

			int index = 0;

			Position = SaveHelper.LoadCubePosition(loadBytes, ref index).InWorldSpace(null) + new Vector3(0, Cube.CUBE_SCALE, 0);

			Main.camera.Rotation = SaveHelper.LoadVector3(loadBytes, ref index);
			Rotation = Main.camera.Rotation;

			health = SaveHelper.LoadInt32(loadBytes, ref index);
			maxHealth = SaveHelper.LoadInt32(loadBytes, ref index);

			inventory = Inventory.Load(loadBytes, ref index);

			uiPlayer = new UIInventoryPlayer(this, inventory, craftInventory, accessoryInventory);
			currentUI = uiPlayer;

            loadedTimeOfDay = SaveHelper.LoadFloat32(loadBytes, ref index);
            SpawnPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
		}
	}
}
