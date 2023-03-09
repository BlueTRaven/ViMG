using BepuPhysics;
using BepuPhysics.Collidables;
using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Items;
using ViMG.Physics;
using ViMG.UIs;

namespace ViMG
{
	[EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
	[EntityMeta(9, 0)]
	public class Player : Entity, IHitboxOwner
	{
		public enum DamageType
        {
			Unspecified,
			Melee,
			Ranged,
			Magic,
        }

		public struct AccumulatedStats
        {
			public float HPScale;			//% hp increase.
			public int HPFlat;          //flat hp increase. Applied AFTER, unmodified by scale.
			public float MPScale;
			public int MPFlat;
			public float HPRegenTime;
			public int HPRegenAmt;
			public float MPRegenTime;
			public int MPRegenAmt;
			public float MeleeAtkScale;	//added to base scale value (1).
			public float RangeAtkScale;
			public float MagicAtkScale;
			public float MeleeFlat;		//flat damage added on top of scale value. Added AFTER - unmodified by scale.
			public float RangeFlat;
			public float MagicFlat;
			public float MeleeSpdScale;
			public float RangeSpdScale;
			public float MagicSpdScale;
			public float MiningScale;	//TODO implement
			public float DefenseScale;	//% defense increase
			public int DefenseFlat;		//flat defense increase. Applied AFTER, unmodified by scale.
			public float KnockbackResist;
			public float Speed;         //Adds to xz max velocity
            public float RunSpeed;
			public float Acceleration;  //Adds to xz accel
			public float JumpSpeed;
			public int JumpNum;
			public float InvulnTime;
			public float UseSpeed;
			public int DashNum;
			public float DashSpeed;

			public IDashEffect DashEffect;

			public IJumpEffect[] JumpEffects;
			private int currentJumpEffectIndex;

			public void AddJumpEffect(IJumpEffect effect)
            {
				if (currentJumpEffectIndex >= 4)
					return;

				JumpEffects[currentJumpEffectIndex++] = effect;
            }

            public static AccumulatedStats operator +(AccumulatedStats a, AccumulatedStats b)
            {
				var stats = new AccumulatedStats()
				{
					JumpEffects = a.JumpEffects,
					currentJumpEffectIndex = a.currentJumpEffectIndex,
					HPFlat = a.HPFlat + b.HPFlat,
					HPScale = a.HPScale + b.HPScale,
					MPFlat = a.MPFlat + b.MPFlat,
					MPScale = a.MPScale + b.MPScale,
					HPRegenTime = a.HPRegenTime + b.HPRegenTime,
					HPRegenAmt = a.HPRegenAmt + b.HPRegenAmt,
					MPRegenTime = a.MPRegenTime + b.MPRegenTime,
					MPRegenAmt = a.MPRegenAmt + b.MPRegenAmt,
					MiningScale = a.MiningScale + b.MiningScale,
					DefenseScale = a.DefenseScale + b.DefenseScale,
					DefenseFlat = a.DefenseFlat + b.DefenseFlat,
					MeleeAtkScale = a.MeleeAtkScale + b.MeleeAtkScale,
					RangeAtkScale = a.RangeAtkScale + b.RangeAtkScale,
					MagicAtkScale = a.MagicAtkScale + b.MagicAtkScale,
					MeleeFlat = a.MeleeFlat + b.MeleeFlat,
					RangeFlat = a.RangeFlat + b.RangeFlat,
					MagicFlat = a.MagicFlat + b.MagicFlat,
					MeleeSpdScale = a.MeleeSpdScale + b.MeleeSpdScale,
					RangeSpdScale = a.RangeSpdScale + b.RangeSpdScale,
					MagicSpdScale = a.MagicSpdScale + b.MagicSpdScale,
					KnockbackResist = a.KnockbackResist + b.KnockbackResist,
					Speed = a.Speed + b.Speed,
					RunSpeed = a.RunSpeed + b.RunSpeed,
					Acceleration = a.Acceleration + b.Acceleration,
					JumpSpeed = a.JumpSpeed + b.JumpSpeed,
					JumpNum = a.JumpNum + b.JumpNum,
					InvulnTime = a.InvulnTime + b.InvulnTime,
					UseSpeed = a.UseSpeed + b.UseSpeed,
				};
				
				return stats;
            }
        }

        public const float INTERACT_DISTANCE = Cube.CUBE_SCALE * 4.5f;

		private enum State
		{
			Noclip,
			Normal,
			Swimming,
			Attack,
			Dash,
			Hurt,
			Dead,
		}

		public CubePosition SpawnPosition;
		private float loadedTimeOfDay = -1;

		public Vector3 Rotation;

		//public Vector3 Velocity;

		private float moveSpeed = Cube.CUBE_SCALE * 0.8f;
		public Vector3 MaxVelocity = Cube.CUBE_SCALE * new Vector3(3.2f, 17, 3.2f);
		public Vector3 MaxVelocitySwimming = new Vector3(2.8f) * Cube.CUBE_SCALE;
		public Vector3 MaxVelocitySwimmingFast = new Vector3(5.6f) * Cube.CUBE_SCALE;
		public float MaxFallVelocity;
		private float fallStartY;   //the upper-most point of the current jump. If the player hits something > FALL_HEIGHT_FATAL, they will die.
		private const float FALL_HEIGHT_DAMAGE_START = Cube.CUBE_SCALE * 5;
		private const float FALL_HEIGHT_FATAL = Cube.CUBE_SCALE * 18;

		private int currentJumps;
		public float JumpSpeed = 10f * Cube.CUBE_SCALE;
		public bool IsRunning;

		private MouseState currentMS;
		private MouseState previousMS;
		private Vector2 previousMousePosition;

		private State state;

		//private bool onGround;
		private bool inRope;
		private bool inWater;
		private bool headUnderWater;

		private TypedIndex physicsShapeIndex;
		private BodyHandle physicsHandle;
		private ContactChecker contactChecker;

		private Rectangle3D Bounds => new Rectangle3D(Position + new Vector3(-Cube.CUBE_SCALE * 0.85f / 2f, -Cube.CUBE_SCALE * 2f, -Cube.CUBE_SCALE * 0.85f / 2f),
			new Vector3(Cube.CUBE_SCALE * 0.85f, Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 0.85f));
		private int hurtbox = -1;
		private const float INVULN_TIME = 2f;
		private float invulnTimer;
		private float inputLockupTimer;
		private float damageAnimTimer;
		private const float DAMAGE_ANIM_TIME = 15f / 60f;

		private int hitbox = -1;
		private DamageType hitboxDamageType = DamageType.Unspecified;
		private Vector3 hitboxOffset;
		private float hitboxTimer;
		private float hitboxSize;
		private const float HITBOX_TIME = 3f / Main.FIXED_FPS;
		private float attackStateTimer;
		private float attackStateMoveTimer;
		private int attackStateInitiatedWeapon;	//the weapon that initiated the attack state.
		private float itemUseCooldownTimer;
		private float useTimer;
		private const float ATTACK_TIME = 0.5f;

		private int numDashes;
		private float dashResetTimer;
		private const float DASH_RESET_TIME = 0.5f;
		private int dashSubstate;
		private float dashTimer;
		private float dashTime;
		private Vector3 dashDirection;
		private float dashDoublePressTimer;
		private const float DOUBLEPRESS_DURATION = 1f / 4f;

		private float deadTimer;
		private const float DEAD_TIME = 3f;

		private World.RaycastResult lookAtResult;
		//Is currently looking at a cube or not
		public bool IsLooking;
		public bool CanPlace;
		//position that the player is currently looking at (if any), in cube space.
		//Will be the position of the last looked at object if nothing is currently looked at.
		public CubePosition LookAtPos;
		public CubePosition LookAtEnd;
		public Vector3 LookAtNormal;
		//likewise, this is the position the player will place a cube if they right clicked the LookAtPos
		//with a cube item in hand.
		public CubePosition PlaceAtPos;	
		private float alive = 0;

		private const float PULL_RADIUS = 3.25f * Cube.CUBE_SCALE;
		private const float NEUTRAL_RADIUS = 2.5f * Cube.CUBE_SCALE;
		private const float PUSH_RADIUS = 1.75f * Cube.CUBE_SCALE;
		private Vector3 attackStateTargetPos;

		private SimpleMesh<VertexCube, int> lookAtMesh;

		public const int INVENTORY_ROWS = 4;
		public const int INVENTORY_COLUMNS = 8;

		private Inventory inventory;
		private Inventory craftInventory;
		private Inventory gearInventory;
		private Inventory accessoryInventory;
		//private Menu currentUI;
		private MenuPlayer menuPlayer;

		public int Health;
		public int MaxHealth = 20;
		public int Magic;
		public int MaxMagic = 5;

		//Toggled when pressing ctrl
		//Whether or not to use the "expanded"/full-size mining space for pickaxes
		public bool ExpandedMineState = true;

		private float healthRegenTimer;
		private float magicRegenTimer;

		private AccumulatedStats stats;
		private SetBonus setBonus;

		private bool hasMoved;
		private bool hasRotated;

		private BuffManagerPlayer buffManager;

		private bool respawnInit;
		private Player respawnPlayer;

		public Player()
		{
			AlwaysRender = true;

			//TODO serialize this maybe?
			buffManager = new BuffManagerPlayer(this);
		}

		//Creates a new player from a dead player.
		public Player(Player deadPlayer)
		{
			AlwaysRender = true;

			respawnInit = true;
			respawnPlayer = deadPlayer;

			//TODO serialize this maybe?
			buffManager = new BuffManagerPlayer(this);
		}

		public void FirstCreated()
		{
			inventory ??= new Inventory(INVENTORY_ROWS * INVENTORY_COLUMNS);
			accessoryInventory ??= new Inventory(6);
			gearInventory ??= new Inventory(10);	
			
			inventory.Add(ItemPickaxe.CreatePickaxe(new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_head_tin"), 1, 1)));//new ItemInstance(Main.Registry.ItemRegistry.Get("pickaxe_base"), 1, 1));
			inventory.Add(ItemSword.CreateSword(new ItemInstance(Main.Registry.ItemRegistry.Get("sword_blade_tin"), 1, 1)));
		}

        public override void Initialize(World world)
        {
            base.Initialize(world);

			if (!respawnInit)
			{
				Options.CenterMouse();
				currentMS = Mouse.GetState();
				previousMS = currentMS;
				previousMousePosition = new Vector2(currentMS.X, currentMS.Y);

				Rotation = Main.camera.Rotation;

				craftInventory = new Inventory(8);
				//Start with 10 gear slots so we don't have to worry about expanding in the future.
				//For now, we only have 3:
				//Heart, boots, and feather artefact. 
			}
            else
            {
				invulnTimer = 6f;   //6 seconds of invuln after respawning

				inventory = respawnPlayer.inventory;
				accessoryInventory = respawnPlayer.accessoryInventory;
				gearInventory = respawnPlayer.gearInventory;

				SpawnPosition = respawnPlayer.SpawnPosition;
				Position = respawnPlayer.SpawnPosition.InWorldSpace();

				respawnPlayer = null;
				respawnInit = false;

				//Options.CenterMouse();
				currentMS = Mouse.GetState();
				previousMS = currentMS;
				previousMousePosition = new Vector2(currentMS.X, currentMS.Y);

				Rotation = Main.camera.Rotation;

				craftInventory = new Inventory(8);

				Health = MaxHealth / 4;
			}

			hurtbox = -1;
			hitbox = -1;
			Health = MaxHealth;
			Magic = MaxMagic;

			var physicsShape = new Capsule(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE * 0.98f);
			physicsShapeIndex = world.PhysicsInfo.Simulation.Shapes.Add(physicsShape);
			physicsHandle = world.PhysicsInfo.Simulation.Bodies.Add(BodyDescription.CreateDynamic(
				new RigidPose(Position.ToNumerics()), new BodyInertia() { InverseMass = 1f / 20f }, physicsShapeIndex, 0.001f));
			contactChecker = new ContactChecker();

			world.PhysicsInfo.Properties[physicsHandle] = new PhysicsProperties(new SubgroupCollisionFilter(FilterGroups.GROUP_PLAYER, 0), 1f);

			menuPlayer = new MenuPlayer(world.GameStateManager, this, inventory, craftInventory, accessoryInventory, gearInventory);
			menuPlayer.Close();
			world.GameStateManager.TheIsland.SetMenu(menuPlayer);

			//If we loaded the time of day, set the world's time of day to it.
			if (loadedTimeOfDay > 0)
			{
				world.SetTime(loadedTimeOfDay);
				loadedTimeOfDay = -1;
			}

			//SpawnPosition got corrupted or something or is a version that doesn't have it
			if (SpawnPosition == new CubePosition())
			{
				SpawnPosition = world.ChunkManager.ThreadedView.GetFirstSolidDown(new CubePosition(world.sizeInCubes / 2, world.sizeInCubes, world.sizeInCubes / 2)).GetOrDefault(new CubePosition());
			}
		}

        public override void OnDelete()
        {
            base.OnDelete();

			for (int i = 0; i < craftInventory.NumSlots; i++)
			{
				if (craftInventory.Get(i).valid)
				{
					EntityItem ent = new EntityItem(Position, new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5), Cube.CUBE_SCALE * 1.6f,
						Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5)), craftInventory.Get(i));
					world.EntityManager.Add(ent);
				}
			}

			//TODO death screen and stuff
			Player p = new Player(this);
			world.EntityManager.Add(p);
			world.player = p;
		}

        public override void OnUnload()
        {
            base.OnUnload();

			if (hitbox != -1)
				world.HitboxManager.Remove(hitbox);
			hitbox = -1;

			world.PhysicsInfo.Simulation.Bodies.Remove(physicsHandle);
			world.PhysicsInfo.Simulation.Shapes.Remove(physicsShapeIndex);

			physicsHandle = new BodyHandle();
			physicsShapeIndex = new TypedIndex();
		}

        public override void Update(double deltaTime)
		{
			if (state != State.Noclip)
				Position = world.PhysicsInfo.Simulation.Bodies[physicsHandle].Pose.Position + new Vector3(0, Cube.CUBE_SCALE * 0.6f, 0);

			hasMoved = false;
			hasRotated = false;

			if (Main.inputManager.JustPressed(Keys.G))
				Main.Debug = !Main.Debug;

			if (Main.Debug)
				state = State.Noclip;
			else if (state == State.Noclip)
				state = State.Normal;

			if (!Main.Debug && (!world.ChunkManager.IsInWorldBounds(Position) ||
				!world.ChunkLoadManager.IsLoaded(ChunkPosition.WorldSpaceChunk(Position))))
				world.PhysicsInfo.Simulation.Sleeper.Sleep(world.PhysicsInfo.Simulation.Bodies[physicsHandle].MemoryLocation.Index);
			else
				world.PhysicsInfo.Simulation.Awakener.AwakenBody(physicsHandle);    //player physics shape can never fall asleep

			if (hurtbox == -1)
				hurtbox = world.HitboxManager.Add(this, Bounds, Vector3.Zero, HitboxManager.Group.PLAYER_TAKE, -1, -1f, invulnTimer <= 0);
			else if (state != State.Noclip)
				world.HitboxManager.Update(hurtbox, Bounds, invulnTimer <= 0);

			UpdateStats(deltaTime);

			if (inputLockupTimer <= 0)
			{
				healthRegenTimer -= (float)deltaTime;
				magicRegenTimer -= (float)deltaTime;

				if (healthRegenTimer <= 0)
                {
					healthRegenTimer = 10 - (5 * stats.HPRegenTime);
					Heal(stats.HPRegenAmt);	//minimum of 0 hpregen; we will never regen unless stats have been increased
                }

				if (magicRegenTimer <= 0)
                {
					magicRegenTimer = 10 - (5 * stats.MPRegenTime);
					Magic += stats.MPRegenAmt + 1;	//minimum of 1 mana regen; we will always regen even if stats are unaffected

					if (Magic > MaxMagic)
						Magic = MaxMagic;
                }
			}

			if (state == State.Noclip)
			{
				UpdateMovement(deltaTime);
			}
			else if (state == State.Dead)
            {
				deadTimer -= (float)deltaTime;

				if (deadTimer <= 0)
					KillWithoutAnimation();
            }
			else if (state == State.Normal)
			{
				if (Main.inputManager.JustPressed(Keys.LeftControl))
					ExpandedMineState = !ExpandedMineState;

				invulnTimer -= (float)deltaTime;

				if (inWater)
				{
					UpdateMovementWater(deltaTime);
				}
				else
				{
					if (inRope)
					{
						UpdateMovementRope(deltaTime);
					}
					else
					{
						UpdateMovement(deltaTime);
					}
				}

				//UpdateCollision(deltaTime);
				
				//Position += Velocity * (float)deltaTime;
			}
			else if (state == State.Dash)
            {
				//always invulnerable during a dash?
				invulnTimer = 0.01f;

				UpdateDash(deltaTime);

				//UpdateCollision(deltaTime);

				//Position += Velocity * (float)deltaTime;
            }
			else if (state == State.Attack)
			{
				invulnTimer -= (float)deltaTime;

				attackStateTimer -= (float)deltaTime;
				attackStateMoveTimer -= (float)deltaTime;

				UpdateMovement(deltaTime);

				//UpdateCollision(deltaTime);

				//Position += Velocity * (float)deltaTime;

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

			contactChecker.Update(world, physicsHandle);

			if (world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear.Length() > float.Epsilon)
				hasMoved = true;

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
					Rectangle3D rect = new Rectangle3D(Position + hitboxOffset - new Vector3(hitboxSize / 2), new Vector3(hitboxSize));

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

			if (Main.inputManager.JustPressed(Keys.F3))
			{
				Main.DebugChunks = !Main.DebugChunks;
			}

			if (Main.inputManager.JustPressed(Keys.E))
			{
				if (world.GameStateManager.GetCurrentGameState().GetCurrentMenu() != menuPlayer)
					world.GameStateManager.GetCurrentGameState().PopMenu();
				else menuPlayer.Toggle();
			}

			if (useTimer <= 0)
			{
				int oldHighlight = menuPlayer.HighlightIndex;
				if (Main.inputManager.JustPressed(Keys.D1))
				{
					menuPlayer.HighlightIndex = 0;
				}

				if (Main.inputManager.JustPressed(Keys.D2))
				{
					menuPlayer.HighlightIndex = 1;
				}

				if (Main.inputManager.JustPressed(Keys.D3))
				{
					menuPlayer.HighlightIndex = 2;
				}

				if (Main.inputManager.JustPressed(Keys.D4))
				{
					menuPlayer.HighlightIndex = 3;
				}

				if (Main.inputManager.JustPressed(Keys.D5))
				{
					menuPlayer.HighlightIndex = 4;
				}

				if (Main.inputManager.JustPressed(Keys.D6))
				{
					menuPlayer.HighlightIndex = 5;
				}

				if (Main.inputManager.JustPressed(Keys.D7))
				{
					menuPlayer.HighlightIndex = 6;
				}

				if (Main.inputManager.JustPressed(Keys.D8))
				{
					menuPlayer.HighlightIndex = 7;
				}

				if (oldHighlight != menuPlayer.HighlightIndex)
				{
					if (inventory.Get(oldHighlight).valid)
					{
						inventory.Get(oldHighlight).item.EndHold(this, inventory, menuPlayer.HighlightIndex);

						if (inventory.Get(menuPlayer.HighlightIndex).valid)
							inventory.Get(oldHighlight).item.StartHold(this, inventory, menuPlayer.HighlightIndex);
					}
				}
			}

			/*if (Main.inputManager.JustPressed(Keys.Escape))
            {
				world.GameStateManager.GetCurrentGameState().PushMenu(new MenuPause(world.GameStateManager, world));
			}*/

			if (inventory.Get(menuPlayer.HighlightIndex).valid)
				inventory.Get(menuPlayer.HighlightIndex).item.Hold(this, inventory, menuPlayer.HighlightIndex);

			lookAtResult = world.Raycast(Position, Position - Main.camera.Forward * INTERACT_DISTANCE,
			(Vector3 pos) =>
			{
				Cube cube = world.ChunkManager.ThreadedView.GetCube(CubePosition.FromWorldSpace(pos)).GetOrDefault(Main.Registry.CubeRegistry.Air);
				bool isLooking = world.ChunkManager.IsInWorldBounds(pos) && cube.Touchable;
				
				//if we're climbing a rope, ignore the rope
				if (inRope)
					isLooking = isLooking && cube.Collision != Cube.CollisionValue.Rope;

				return isLooking;
			});

			IsLooking = false;
			CanPlace = false;
			if (lookAtResult.hasHit)
			{
				if (world.ChunkManager.IsInWorldBounds(lookAtResult.hit))
				{
					var c = world.ChunkManager.ThreadedView.GetCube(CubePosition.FromWorldSpace(lookAtResult.hit));
					IsLooking = true;
					this.LookAtPos = CubePosition.FromWorldSpace(lookAtResult.hit);
					this.LookAtNormal = lookAtResult.normal;
					this.PlaceAtPos = CubePosition.FromWorldSpace(lookAtResult.hit + CubePosition.ToWorldSpaceV3(lookAtResult.normal));

					if (world.ChunkManager.IsInWorldBounds(PlaceAtPos))
						CanPlace = true;
				}
			}
			
			if (world.ChunkManager.IsInWorldBounds(lookAtResult.end))
				this.LookAtEnd = CubePosition.FromWorldSpace(lookAtResult.end);

			//currentUI.Update(null, deltaTime);
			UpdateMouse();

			hitboxTimer -= (float)deltaTime;

			useTimer -= (float)deltaTime;
			itemUseCooldownTimer -= (float)deltaTime;

			alive += (float)deltaTime;
		}

		private IDashEffect dashEffect = new DefaultDashEffect();
		private IJumpEffect[] jumpEffects = new IJumpEffect[4];
		private void UpdateStats(double deltaTime)
		{
			Array.Clear(jumpEffects, 0, 4);

			AccumulatedStats accumulatedStats = new AccumulatedStats();
			accumulatedStats.DashEffect = dashEffect;   //TODO remove this is temporary testing code
			accumulatedStats.DashNum = 1;
			accumulatedStats.DashSpeed = Cube.CUBE_SCALE * 16f;
			accumulatedStats.JumpEffects = jumpEffects;
			SetBonus.SetBonusInstance bonus = new SetBonus.SetBonusInstance();

			for (int i = 0; i < accessoryInventory.NumSlots; i++)
			{
				ItemInstance item = accessoryInventory.Get(i);

				if (item.valid)
				{
					item.item.AccumulateStats(this, accessoryInventory, i, ref accumulatedStats, ref bonus);
				}
			}

			for (int i = 0; i < gearInventory.NumSlots; i++)
            {
				ItemInstance item = gearInventory.Get(i);

				if (item.valid)
				{
					item.item.AccumulateStats(this, gearInventory, i, ref accumulatedStats, ref bonus);
				}
			}

			if (bonus.SetBonus != null && bonus.Count == 3)
			{
				bonus.SetBonus.AccumulateStats(this, ref accumulatedStats);
			}

			setBonus = bonus.SetBonus;
			stats = accumulatedStats;

			buffManager.Update(deltaTime, ref stats);
		}

		private void UpdateMovementRope(double deltaTime)
        {
			/*Vector3 actualMaxVel = MaxVelocity;

			bool movementPressed = false;
			Vector2 velXY = new Vector2(Velocity.X, Velocity.Z);

			if (inputLockupTimer <= 0 && world.GameStateManager.GetCurrentGameState().GetCurrentMenu() == menuPlayer && !menuPlayer.IsOpened)
			{
				actualMaxVel *= new Vector3(1 + stats.Speed, 1, 1 + stats.Speed);

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

				if (Main.inputManager.IsPressed(Keys.Space))
                {
					Velocity.Y -= World.GRAVITY;
					movementPressed = true;
                }

				if (Main.inputManager.IsPressed(Keys.LeftControl))
                {
					Velocity.Y += World.GRAVITY;
					movementPressed = true;
				}

				Vector2 clampXY = new Vector2(actualMaxVel.X, actualMaxVel.Z);
				velXY = new Vector2(Velocity.X, Velocity.Z);

				if (velXY.Length() > clampXY.Length())
				{
					velXY.Normalize();
					velXY *= clampXY.Length();
				}

				if (world.GameStateManager.GetCurrentGameState().GetCurrentMenu() == menuPlayer &&
					!menuPlayer.IsOpened && itemUseCooldownTimer <= 0 && (useTimer <= 0 ||
					Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton) ||
					Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton)))
				{
					if (Main.inputManager.IsPressed(A1r.Input.MouseInput.LeftButton))
					{
						if (inventory.Get(menuPlayer.HighlightIndex).item != null && inventory.Get(menuPlayer.HighlightIndex).item.LeftClick(this, inventory, menuPlayer.HighlightIndex, -Main.camera.Forward, out itemUseCooldownTimer))
							PerformAction();
					}

					if (Main.inputManager.IsPressed(A1r.Input.MouseInput.RightButton))
					{
						var tracker = world.EntityManager.GetEntityTrackingPosition(LookAtPos);
						if (tracker.HasValue() && tracker.Get().OnInteract(this))
							PerformAction();
						else if (inventory.Get(menuPlayer.HighlightIndex).item != null && inventory.Get(menuPlayer.HighlightIndex).item.RightClick(this, inventory, menuPlayer.HighlightIndex, -Main.camera.Forward, out itemUseCooldownTimer))
							PerformAction();
					}
				}

				if (Main.inputManager.JustPressed(Keys.Q))
				{
					if (inventory.Get(menuPlayer.HighlightIndex).valid)
					{
						int num = 1;
						if (Main.inputManager.IsPressed(Keys.LeftControl))
							num = inventory.Get(menuPlayer.HighlightIndex).num;

						ThrowItem(inventory, menuPlayer.HighlightIndex, num);
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

				if (MathF.Abs(Velocity.Y) > 0)
                {
					float decel = Cube.CUBE_SCALE / 2f;
					float signedDecel = decel * -MathF.Sign(Velocity.Y);

					if (MathF.Abs(Velocity.Y) - decel < 0)
						Velocity.Y = 0;
					else Velocity.Y += signedDecel;
                }
			}

			Velocity = new Vector3(velXY.X, Velocity.Y, velXY.Y);

			if (Velocity.Length() > float.Epsilon)
				hasMoved = true;

			if (Velocity.Y < -actualMaxVel.Y)
				Velocity.Y = -actualMaxVel.Y;
			if (Velocity.Y > actualMaxVel.Y / 4f)
				Velocity.Y = actualMaxVel.Y / 4f;*/
		}

		private void UpdateMovementWater(double deltaTime)
		{
			/*Vector3 actualMaxVel = MaxVelocitySwimming;

			bool movementPressed = false;
			bool jumpHeld = false;
			bool swimmingFast = false;

			fallStartY = Position.Y;

			if (inputLockupTimer <= 0 && !menuPlayer.IsOpened)
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

				if (world.GameStateManager.GetCurrentGameState().GetCurrentMenu() == menuPlayer && 
					!menuPlayer.IsOpened && itemUseCooldownTimer <= 0 && (useTimer <= 0 ||
					Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton) ||
					Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton)))
				{
					if (Main.inputManager.IsPressed(A1r.Input.MouseInput.LeftButton))
					{
						if (inventory.Get(menuPlayer.HighlightIndex).item != null && inventory.Get(menuPlayer.HighlightIndex).item.LeftClick(this, inventory, menuPlayer.HighlightIndex, -Main.camera.Forward, out itemUseCooldownTimer))
							PerformAction();
					}

					if (Main.inputManager.IsPressed(A1r.Input.MouseInput.RightButton))
					{
						var tracker = world.EntityManager.GetEntityTrackingPosition(LookAtPos);
						if (tracker.HasValue() && tracker.Get().OnInteract(this))
							PerformAction();
						else if (inventory.Get(menuPlayer.HighlightIndex).item != null && inventory.Get(menuPlayer.HighlightIndex).item.RightClick(this, inventory, menuPlayer.HighlightIndex, -Main.camera.Forward, out itemUseCooldownTimer))
							PerformAction();
					}
				}

				if (Main.inputManager.JustPressed(Keys.Q))
				{
					if (inventory.Get(menuPlayer.HighlightIndex).valid)
					{
						int num = 1;
						if (Main.inputManager.IsPressed(Keys.LeftControl))
							num = inventory.Get(menuPlayer.HighlightIndex).num;

						ThrowItem(inventory, menuPlayer.HighlightIndex, num);
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
				Velocity.Y = actualMaxVel.Y;*/
		}

		private void UpdateMovement(double deltaTime)
		{
			if (state == State.Noclip)
			{
				world.PhysicsInfo.Simulation.Bodies[physicsHandle].Pose.Position = Position.ToNumerics();
				world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear = Vector3.Zero.ToNumerics();

				const float MIN_CAM_SPEED = Cube.CUBE_SCALE / 4f;
				const float MAX_CAM_SPEED = MIN_CAM_SPEED * 8;

				float moveSpeed = MIN_CAM_SPEED;

				if (Main.inputManager.IsHeld(Keys.LeftShift))
					moveSpeed = MAX_CAM_SPEED;

				Vector3 oldPos = Position;

				if (Main.inputManager.IsPressed(Keys.W))
					Position -= Vector3.Normalize(Main.camera.ForwardYawOnly) * moveSpeed;
				if (Main.inputManager.IsPressed(Keys.S))
					Position += Vector3.Normalize(Main.camera.ForwardYawOnly) * moveSpeed;
				if (Main.inputManager.IsPressed(Keys.A))
					Position -= Vector3.Normalize(Main.camera.Right) * moveSpeed;
				if (Main.inputManager.IsPressed(Keys.D))
					Position += Vector3.Normalize(Main.camera.Right) * moveSpeed;
				if (Main.inputManager.IsPressed(Keys.Space))
					Position += Vector3.Up * moveSpeed;
				if (Main.inputManager.IsPressed(Keys.LeftControl))
					Position -= Vector3.Up * moveSpeed;

				if (Position != oldPos)
					hasMoved = true;

				UpdatePerformAction();

				fallStartY = Position.Y;	//so we don't immediately die sometimes
			}
			else
			{
				Vector3 actualMaxVel = MaxVelocity;

				bool movementPressed = false;
				//Vector2 velXY = new Vector2(Velocity.X, Velocity.Z);
				Vector3 velocity = world.PhysicsInfo.Simulation.Bodies[physicsHandle].Dynamics.Motion.Velocity.Linear;

				if (Main.inputManager.JustReleased(Keys.LeftShift))
					IsRunning = false;

				if (inputLockupTimer <= 0 && world.GameStateManager.GetCurrentGameState().GetCurrentMenu() == menuPlayer && !menuPlayer.IsOpened)
				{
					if (contactChecker.OnGround && Main.inputManager.IsHeld(Keys.LeftShift))
						IsRunning = true;

					float actualAcceleration = moveSpeed + stats.Acceleration;

					if (IsRunning)
					{
						actualMaxVel *= 1 + stats.RunSpeed;
						actualAcceleration *= 2;
					}

					actualMaxVel *= new Vector3(1 + stats.Speed, 1, 1 + stats.Speed);
					
					if (Main.inputManager.IsPressed(Keys.W))
					{
						velocity -= Vector3.Normalize(Main.camera.ForwardYawOnly) * actualAcceleration;
						movementPressed = true;
					}
					if (Main.inputManager.IsPressed(Keys.S))
					{
						velocity += Vector3.Normalize(Main.camera.ForwardYawOnly) * actualAcceleration;
						movementPressed = true;
					}
					if (Main.inputManager.IsPressed(Keys.A))
					{
						velocity -= Vector3.Normalize(Main.camera.Right) * actualAcceleration;
						movementPressed = true;
					}
					if (Main.inputManager.IsPressed(Keys.D))
					{
						velocity += Vector3.Normalize(Main.camera.Right) * actualAcceleration;
						movementPressed = true;
					}
					if ((contactChecker.OnGround || currentJumps > 0) && Main.inputManager.JustPressed(Keys.Space))
					{
						hasMoved = true;
						if (!contactChecker.OnGround)
						{
							stats.JumpEffects[stats.JumpNum - currentJumps].DoJump(this, JumpSpeed + stats.JumpSpeed, ref velocity);

							currentJumps--;
						}
						else
						{
							velocity.Y = JumpSpeed + stats.JumpSpeed;
						}
					}

					Vector2 clampXY = new Vector2(actualMaxVel.X, actualMaxVel.Z);
					Vector2 velXY = new Vector2(velocity.X, velocity.Z);

					if (velXY.Length() > clampXY.Length())
					{
						velXY.Normalize();
						velXY *= clampXY.Length();
					}

					velocity = new Vector3(velXY.X, velocity.Y, velXY.Y);

					UpdatePerformAction();

					if (Main.inputManager.JustPressed(Keys.Q))
					{
						if (inventory.Get(menuPlayer.HighlightIndex).valid)
						{
							int num = 1;
							if (Main.inputManager.IsPressed(Keys.LeftControl))
								num = inventory.Get(menuPlayer.HighlightIndex).num;

							ThrowItem(inventory, menuPlayer.HighlightIndex, num);
						}
					}
				}

				if (movementPressed)
					hasMoved = true;
				/*if (!movementPressed)
				{
					if (velXY.Length() > 0)
					{
						float decel = Cube.CUBE_SCALE / 2f;

						if (!onGround)
							decel = Cube.CUBE_SCALE / 8f;

						velXY = Vector2.Normalize(velXY) * MathF.Max(velXY.Length() - decel, 0);
					}
				}*/

				if (velocity.Length() > float.Epsilon)
					hasMoved = true;

				if (velocity.Y > Cube.CUBE_SCALE * 3.2f && Main.inputManager.JustReleased(Keys.Space))
					velocity.Y = Cube.CUBE_SCALE * 3.2f;

				world.PhysicsInfo.Simulation.Bodies[physicsHandle].Dynamics.Motion.Velocity.Linear = velocity.ToNumerics();
			}

			UpdateMaybeDash(deltaTime);

			if (Main.inputManager.JustPressed(Keys.T))
			{
				world.AddTime(World.DAY_CYCLE_TIME * 0.25f);
			}

			if (Main.inputManager.JustPressed(Keys.V))
            {
				//world.EntityManager.Add(new CaveSalamander(Position - Main.camera.Forward * Cube.CUBE_SCALE * 5f));
				//world.EntityManager.Add(new GenericExplosion(Position - Main.camera.Forward * Cube.CUBE_SCALE * 5f, HitboxManager.Group.PLAYER_DEAL, 1, 1, Cube.CUBE_SCALE * 2f));
			}
		}

		private void UpdatePerformAction()
		{
            if (world.GameStateManager.GetCurrentGameState().GetCurrentMenu() == menuPlayer &&
                    !menuPlayer.IsOpened && itemUseCooldownTimer <= 0 && (useTimer <= 0 ||
                        Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton) ||
                        Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton)))
            {
                if (Main.inputManager.IsPressed(A1r.Input.MouseInput.LeftButton))
                {
                    if (inventory.Get(menuPlayer.HighlightIndex).item != null && inventory.Get(menuPlayer.HighlightIndex).item.LeftClick(this, inventory, menuPlayer.HighlightIndex, -Main.camera.Forward, out itemUseCooldownTimer))
                        PerformAction();
                }

                if (Main.inputManager.IsPressed(A1r.Input.MouseInput.RightButton))
                {
                    bool performedAction = false;
                    var entityTracking = world.EntityManager.GetEntityTrackingPosition(LookAtPos).GetOrDefault(null);

                    if (entityTracking != null)
                    {
                        if (entityTracking is ICubeTracker tracker)
                        {
                            if (tracker.OnInteract(this))
                            {
                                PerformAction();
                                performedAction = true;
                            }
                        }
                        else if (entityTracking is IMultiCubeTracker multiTracker)
                        {
                            if (multiTracker.OnInteract(this))
                            {
                                PerformAction();
                                performedAction = true;
                            }
                        }
                    }

                    if (!performedAction && inventory.Get(menuPlayer.HighlightIndex).item != null && inventory.Get(menuPlayer.HighlightIndex).item
                        .RightClick(this, inventory, menuPlayer.HighlightIndex, -Main.camera.Forward, out itemUseCooldownTimer))
                        PerformAction();
                }
            }
        }

		private void UpdateMaybeDash(double deltaTime)
        {
			if (contactChecker.OnGround)
            {
				dashResetTimer -= (float)deltaTime;
            }

			if (dashResetTimer <= 0)
			{
				if (stats.DashEffect != null && stats.DashNum > 0)
				{
					dashDoublePressTimer -= (float)deltaTime;

					if (dashSubstate == 0)
					{
						if (Main.inputManager.JustPressed(Keys.LeftShift))
						{
							dashSubstate++;
							dashDoublePressTimer = DOUBLEPRESS_DURATION;
						}
					}
					else if (dashSubstate == 1)
					{
						if (dashDoublePressTimer >= 0)
						{
							if (Main.inputManager.JustPressed(Keys.LeftShift))
							{
								state = State.Dash;

								Vector3 vel = world.PhysicsInfo.Simulation.Bodies[physicsHandle].Dynamics.Motion.Velocity.Linear;
								stats.DashEffect.StartDash(this, ref vel, out dashDirection, out dashTime, in stats);
								world.PhysicsInfo.Simulation.Bodies[physicsHandle].Dynamics.Motion.Velocity.Linear = vel.ToNumerics();
								dashTimer = dashTime;

								dashSubstate = 0;
							}
						}
						else
						{
							dashSubstate = 0;
						}
					}
				}
			}
        }

		private void UpdateDash(double deltaTime)
        {
			//in the case where our dash effect suddenly becomes null, just stop dashing I guess?
			if (stats.DashEffect == null)
            {
				state = State.Normal;
				dashSubstate = 0;
				return;
            }

			dashTimer -= (float)deltaTime;

			Vector3 vel = world.PhysicsInfo.Simulation.Bodies[physicsHandle].Dynamics.Motion.Velocity.Linear;
			stats.DashEffect.DoDash(this, ref vel, ref dashDirection, in stats);
			world.PhysicsInfo.Simulation.Bodies[physicsHandle].Dynamics.Motion.Velocity.Linear = vel.ToNumerics();

			hasMoved = true;

			if (dashTimer <= 0)
            {
				dashResetTimer = DASH_RESET_TIME;
				state = State.Normal;
				dashSubstate = 0;
            }
        }

		public void ThrowItem(Inventory inventory, int index, int num)
		{
			if (inventory.Get(menuPlayer.HighlightIndex).valid)
			{
				ItemInstance thrownInstance = new ItemInstance(inventory.Get(index), num);
				EntityItem ent = new EntityItem(Position, -Main.camera.Forward * Cube.CUBE_SCALE * 5, thrownInstance);
				world.EntityManager.Add(ent);

				inventory.Remove(index, num);

				if (!inventory.Get(index).valid || inventory.Get(index).num == 0)
					thrownInstance.item.EndHold(this, inventory, index);
			}
		}

		private void UpdateItemPickup()
		{
			const float suckRadius = Cube.CUBE_SCALE * 3f;
			const float pickupRadius = Cube.CUBE_SCALE * 1.85f;

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

							menuPlayer.AddPickedUpItem(item.Item);
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
			if (contactChecker.OnGround && !contactChecker.WasOnGround)
				LandOnGround();

			/*inRope = false;
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

			int checkCount = (far.X - near.X + 1) * (far.Y - near.Y + 1) * (far.Z - near.Z + 1);
			int pi = 0;
			Span<CubePosition> positions = stackalloc CubePosition[checkCount];
			Span<ushort> ids = stackalloc ushort[checkCount];

			for (int x = near.X; x <= far.X; x++)
			{
				for (int y = near.Y; y <= far.Y; y++)
				{
					for (int z = near.Z; z <= far.Z; z++)
					{
						CubePosition pos = new CubePosition(x, y, z);
						positions[pi] = pos;
						pi++;
					}
				}
			}

			world.ChunkManager.ThreadedView.GetIds(positions, ids, ThreadedCubeView.SafetyCheck.InWorldBounds);

			for (int j = 0; j < checkCount; j++)
            {
				CubePosition pos = positions[j];

				Cube cube = Main.Registry.CubeRegistry.GetOrDefault(ids[j], Main.Registry.CubeRegistry.Air);

				if (cube.Id != 0 &&
					(cube.Collision == Cube.CollisionValue.Collidable ||
					cube.Collision == Cube.CollisionValue.LiquidWater ||
					cube.Collision == Cube.CollisionValue.Rope ||
					cube.Collision == Cube.CollisionValue.Platform))
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
						else if (cube.Collision == Cube.CollisionValue.Rope)
						{
							if (CollisionHelper.CheckCollision(cubeBounds, lowerCheckPos, RADIUS, out Vector3 lowerChange))
							{
								inRope = true;
								collided = true;
							}

							if (CollisionHelper.CheckCollision(cubeBounds, upperCheckPos, RADIUS, out Vector3 upperChange))
							{
								inRope = true;
								collided = true;
							}

							if (collided)
								fallStartY = Position.Y;
						}
						else if (cube.Collision == Cube.CollisionValue.Platform)
						{
							//Only perform this logic if LCtrl is not held.
							if (!Main.inputManager.IsPressed(Keys.LeftControl) && CollisionHelper.CheckCollision(cubeBounds, lowerCheckPos, RADIUS, out Vector3 lowerChange))
							{
								if (lowerChange.Y > 0 && Velocity.Y <= 0)
								{
									Position.Y = lowerCheckPos.Y + Bounds.Size.Y - LOWER_OFFSET + lowerChange.Y;
									LandOnGround();
								}

								collided = true;
							}
						}
						else
						{
							if (CollisionHelper.CheckCollision(cubeBounds, lowerCheckPos, RADIUS, out Vector3 lowerChange))
							{
								Position = lowerCheckPos + new Vector3(0, Bounds.Size.Y - LOWER_OFFSET, 0) + lowerChange;

								if (lowerChange.Y > 0 && Velocity.Y <= 0)
									LandOnGround();
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
			}*/
		}

		private void LandOnGround()
        {
			//Velocity.Y = 0;
			//onGround = true;

			currentJumps = stats.JumpNum;

			float fallDistance = fallStartY - Position.Y;
			if (fallDistance > FALL_HEIGHT_FATAL)
				Kill();
			else
			{
				if (fallDistance > FALL_HEIGHT_DAMAGE_START)
				{
					float t = (fallDistance - FALL_HEIGHT_DAMAGE_START) / (FALL_HEIGHT_FATAL - FALL_HEIGHT_DAMAGE_START);

					int damage = (int)((float)GetRealMaxHealth() * t);

					Damage(damage);
				}

				fallStartY = Position.Y;
			}
		}

		private void UpdateMouse()
		{
			if (hasMoved)
				Main.camera.Position = Position;

			if (menuPlayer.IsOpened || world.GameStateManager.GetCurrentGameState().GetCurrentMenu() != menuPlayer)
				return;

			currentMS = Mouse.GetState();

			if (currentMS != previousMS)
			{
				float scalar = 0.25f;

				Vector3 camRotation = Rotation;

				Vector2 delta = (Options.CurrentWindowResolution.ToVector2() / 2f) - new Vector2(currentMS.X, currentMS.Y);
				previousMS = currentMS;
				previousMousePosition = new Vector2(currentMS.X, currentMS.Y);

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
			useTimer = itemUseCooldownTimer;
			//useTimer = state == State.Noclip ? 0.05f : ATTACK_TIME;
		}

		public void PerformAttack(DamageType damageType, ref float cooldownTimer, ref int damage, ref float knockback)
		{
			//Velocity.X = -Main.camera.Forward.X * 512f;

			//Velocity.Z = -Main.camera.Forward.Z * 512f;

			/*var hitboxes = world.HitboxManager.GetAll();

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
			}*/

			float scale = 0;

			if (damageType == DamageType.Melee)
			{
				scale = stats.MeleeSpdScale;

				/*if (onGround)
					Velocity.Y = -Main.camera.Forward.Y * 3.2f * Cube.CUBE_SCALE;
				else if (Velocity.Y > Cube.CUBE_SCALE)
					Velocity.Y = Cube.CUBE_SCALE;*/
			}
			else if (damageType == DamageType.Ranged)
				scale = stats.RangeSpdScale;
			else if (damageType == DamageType.Magic)
				scale = stats.MagicSpdScale;

			damage = DealDamageCalculation(damageType, damage);

			for (int i = 0; i < accessoryInventory.NumSlots; i++)
            {
				if (accessoryInventory.Get(i).valid)
					accessoryInventory.Get(i).item.OnAttack(this, inventory, menuPlayer.HighlightIndex);
            }

			cooldownTimer -= (cooldownTimer * scale);
			this.attackStateTimer = cooldownTimer;
			this.attackStateMoveTimer = 1f / Main.FIXED_FPS;

			state = State.Attack;
		}

		public void SpawnHitbox(int inventorySlot, int damage, DamageType damageType, Vector3 direction, float knockback = 1, float hitboxSize = Cube.CUBE_SCALE * 1.75f, Buff.BuffInstance[] applyBuffs = null)
		{
			if (hitbox != -1)
				world.HitboxManager.Remove(hitbox);
			this.hitboxDamageType = damageType;

			float offset = hitboxSize + (Cube.CUBE_SCALE / 2f) - (hitboxSize / 2f);
			hitboxOffset = direction * offset;

			Rectangle3D rect = new Rectangle3D(Position + hitboxOffset, new Vector3(hitboxSize));
			this.hitboxSize = hitboxSize;

			hitbox = world.HitboxManager.Add(this, rect, -Main.camera.Forward, HitboxManager.Group.PLAYER_DEAL, DealDamageCalculation(damageType, damage), knockback, 
				applyBuffs: applyBuffs, inventorySlot: inventorySlot);

			hitboxTimer = HITBOX_TIME;

			useTimer = ATTACK_TIME;
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			if (inventory.Get(menuPlayer.HighlightIndex).item != null)
			{
				inventory.Get(menuPlayer.HighlightIndex).item.DrawInHand(device, inventory.Get(menuPlayer.HighlightIndex), this, -Main.camera.Forward);
			}

			if (lookAtMesh == null)
			{
				lookAtMesh = MeshHelper.MakeCubeVertexPositionColorTextureNormal(device, Vector3.Zero, new Vector3(Cube.CUBE_SCALE), MeshHelper.CubeFace.ALL, Color.White, DrawHelper.WhitePixel);
				//lookAtMesh = MeshHelper.MakeCubeVertexPositionColor(device, Vector3.Zero, new Vector3(Cube.CUBE_SCALE), MeshHelper.CubeFace.ALL, Color.White, DrawHelper.WhitePixel);
				lookAtMesh.Name = "Look At Mesh";
			}

			if (lookAtResult.hasHit && world.ChunkManager.IsInWorldBounds(lookAtResult.hit))
			{
				float s = MathF.Sin(MathF.PI * 2f * (alive % 2f)) * 0.5f + 0.5f;
				Color color = Color.Lerp(Color.White, Color.Black, s);

				if (ExpandedMineState && 
					inventory.Get(menuPlayer.HighlightIndex).valid && inventory.Get(menuPlayer.HighlightIndex).item is IHasAreaEffect pickStats)
				{
					CubePosition[] positions = pickStats.GetAffectedPositions(this, inventory.Get(menuPlayer.HighlightIndex), Position, LookAtPos.InWorldSpace(), lookAtResult.normal, out _);
					Span<ushort> ids = stackalloc ushort[positions.Length];

					world.ChunkManager.ThreadedView.GetIds(positions.AsSpan(), ids);

					for (int i = 0; i < positions.Length; i++)
					{
						if (pickStats.CanPredictAir() || Main.Registry.CubeRegistry.GetOrDefault(ids[i], Main.Registry.CubeRegistry.Air).Touchable)
						{
							Main.Renderer.DrawsTransparentPass.Add(new Rendering.RendererDeferred.TransparentDraw((int)lookAtResult.end.Length(),
								Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 2f)) *
								Matrix.CreateScale(1.126f) *
								Matrix.CreateTranslation(new Vector3(Cube.CUBE_SCALE / 2f)) *
								Matrix.CreateTranslation(positions[i].InWorldSpace()),
								Main.assetsManager.GetAsset<Texture2D>("cubes_textures"), DrawHelper.BlackPixel,
								lookAtMesh.VBO, lookAtMesh.IBO, new RectangleF(0, 1008, 16, 16), color));
						}
					}
				}
				else 
				{
					Main.Renderer.DrawsTransparentPass.Add(new Rendering.RendererDeferred.TransparentDraw((int)lookAtResult.end.Length(),
						Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 2f)) *
						Matrix.CreateScale(1.126f) *
						Matrix.CreateTranslation(new Vector3(Cube.CUBE_SCALE / 2f)) *
						Matrix.CreateTranslation(LookAtPos.InWorldSpace()),
						Main.assetsManager.GetAsset<Texture2D>("cubes_textures"), DrawHelper.BlackPixel,
						lookAtMesh.VBO, lookAtMesh.IBO, new RectangleF(0, 1008, 16, 16), color));
				}
			}
		}

		public void DrawUI(SpriteBatch batch)
		{
			if (!Main.inputManager.IsHeld(Keys.F5))
			{
				//currentUI.Draw(batch);

				if (deadTimer > 0 && state == State.Dead)
                {
					float t = 1 - (deadTimer / DEAD_TIME);

					batch.DrawRectangle(new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y), Color.Black * t);
				}

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

		public Inventory GetAccessoryInventory()
        {
			return accessoryInventory;
        }

		public Matrix GetHeldMatrix(float originX, float originY, Vector3 scale)
		{
			float percent = useTimer / ATTACK_TIME;

			if (useTimer <= 0)
				percent = 0;

			float ox = Cube.CUBE_SCALE / 4f - originX;
			float oy = Cube.CUBE_SCALE / 4f - originY;

            Matrix mat =
				Matrix.CreateTranslation(-originX, -originY, 0) *
				Matrix.CreateScale(0.5f * scale) *
				Matrix.CreateRotationZ(MathHelper.ToRadians(35f) * percent) *
				Matrix.CreateRotationY(MathHelper.ToRadians(-45f)) *
				Matrix.CreateRotationX(-Main.camera.Rotation.X) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(Position - Main.camera.Forward * Cube.CUBE_SCALE / 3f + Main.camera.Right * Cube.CUBE_SCALE / 4f - Main.camera.Up * Cube.CUBE_SCALE / 6f);

			return mat;

			/*return Matrix.CreateTranslation(Vector3.Forward * Cube.CUBE_SCALE * 2 + Vector3.Down * Cube.CUBE_SCALE * 1.25f + Vector3.Right * Cube.CUBE_SCALE * 0.85f) *
				Matrix.CreateRotationX(-Main.camera.Rotation.X - MathHelper.ToRadians(35) * percent) *
					Matrix.CreateRotationY(-Main.camera.Rotation.Y - MathHelper.ToRadians(35) * percent) *
					Matrix.CreateRotationZ(-Main.camera.Rotation.Z) *
					Matrix.CreateTranslation(Position);*/
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
				if (us.group == HitboxManager.Group.PLAYER_TAKE && 
					((other.group & HitboxManager.Group.ENEMYHOSTILE_DEAL) == HitboxManager.Group.ENEMYHOSTILE_DEAL || 
					other.group == HitboxManager.Group.NEUTRAL_DEAL))
				{
					if (other.direction.Length() > 0)
					{
						world.PhysicsInfo.Simulation.Bodies[physicsHandle].Dynamics.Motion.Velocity.Linear = 
							(Vector3.Normalize(other.direction) * other.knockback).ToNumerics();
					}
                    else
                    {
						Vector3 direction = Vector3.Normalize(other.bounds.Center - Position);
						world.PhysicsInfo.Simulation.Bodies[physicsHandle].Dynamics.Motion.Velocity.Linear = 
							(direction * other.knockback).ToNumerics();
					}

					state = State.Hurt;

					int amt = TakeDamageCalculation(other);
					Damage(amt);
					buffManager.OnTakeDamage(this, amt, other);

					inputLockupTimer = 0.25f;
					invulnTimer = INVULN_TIME;
				}
			}

			if (us.canInteract && other.canInteract && 
				us.group == HitboxManager.Group.PLAYER_DEAL && (other.group & HitboxManager.Group.ENEMYHOSTILE_TAKE) == HitboxManager.Group.ENEMYHOSTILE_TAKE)
            {
				if (us.inventorySlot >= 0)
				{
					var item = inventory.Get(us.inventorySlot);
					if (item.valid)
					{
						inventory.Get(us.inventorySlot).item.OnDealDamage(this, inventory, us.inventorySlot, other);

						for (int i = 0; i < accessoryInventory.NumSlots; i++)
						{
							if (accessoryInventory.Get(i).valid)
								accessoryInventory.Get(i).item.OnDealDamage(this, inventory, us.inventorySlot, other);
						}
					}
					//This may not be a valid hit; the enemy might be invulnerable
				}
            }
		}

		public void Kill()
        {
			if (state == State.Noclip || state == State.Dead)
				return;

			deadTimer = DEAD_TIME;
			state = State.Dead;
        }

		public void KillWithoutAnimation()
        {
			world.EntityManager.Remove(this);
        }

		private void Damage(int amt)
        {
			if (state == State.Noclip)
				return;

			damageAnimTimer = DAMAGE_ANIM_TIME;

			Health -= amt;

			if (Health <= 0)
			{
				Kill();
			}
		}

		private int TakeDamageCalculation(HitboxManager.Hitbox hitbox)
        {
			float totalDefense = (float)stats.DefenseFlat * (stats.DefenseScale + 1f);
			float defenseCalc = totalDefense * 0.5f;

			float damage = (float)hitbox.damage - defenseCalc;

			if ((int)damage <= 0)
			{
				//If we have enough defense, negate damage entirely. Otherwise, max is 1.
				//Player must have at least 10 defense before this negation can be applied.
				if (totalDefense > 10 && totalDefense > hitbox.damage * 3)
					damage = 0;
				else damage = 1;
			}

			return (int)damage;
        }

		private int DealDamageCalculation(DamageType damageType, int damage)
        {
			float startScale = 1;
			float calculatedDamage = damage;

			if (damageType == DamageType.Melee)
			{
				calculatedDamage *= startScale + stats.MeleeAtkScale;
				calculatedDamage += stats.MeleeFlat;
			}
			else if (damageType == DamageType.Magic)
			{
				calculatedDamage *= startScale + stats.MagicAtkScale;
				calculatedDamage += stats.MagicFlat;
			}
			else if (damageType == DamageType.Ranged)
			{
				calculatedDamage *= startScale + stats.RangeAtkScale;
				calculatedDamage += stats.RangeFlat;
			}
			else calculatedDamage *= startScale;

			return (int)calculatedDamage;
        }

		public void Heal(int amt)
        {
			Health += amt;

			if (Health > GetRealMaxHealth())
				Health = GetRealMaxHealth();
        }

		public BuffManagerPlayer GetBuffManager()
		{
			return buffManager;
		}

		public ref AccumulatedStats GetStats()
        {
			return ref stats;
        }

		public int GetRealMaxHealth()
        {
			int hp = MaxHealth;
			hp += (int)((float)hp * stats.HPScale);
			hp += stats.HPFlat;

			return hp;
        }

		public override void OnSave(List<byte> saveBytes)
		{
			base.OnSave(saveBytes);

			SaveHelper.SaveCubePosition(saveBytes, CubePosition.FromWorldSpace(Position));
			SaveHelper.SaveVector3(saveBytes, Main.camera.Rotation);

			SaveHelper.SaveInt32(saveBytes, Health);
			SaveHelper.SaveInt32(saveBytes, MaxHealth);
			SaveHelper.SaveInt32(saveBytes, Magic);
			SaveHelper.SaveInt32(saveBytes, MaxMagic);

			inventory.Save(saveBytes);
			accessoryInventory.Save(saveBytes);
			gearInventory.Save(saveBytes);

			SaveHelper.SaveFloat32(saveBytes, world.GetTime());
			SaveHelper.SaveCubePosition(saveBytes, SpawnPosition);
		}

		public override void OnLoad(byte[] loadBytes, in int version)
		{
			base.OnLoad(loadBytes, version);

			int index = 0;

			Position = SaveHelper.LoadCubePosition(loadBytes, ref index).InWorldSpace() + new Vector3(0, Cube.CUBE_SCALE, 0);

			Main.camera.Rotation = SaveHelper.LoadVector3(loadBytes, ref index);
			Rotation = Main.camera.Rotation;

			Health = SaveHelper.LoadInt32(loadBytes, ref index);
			MaxHealth = SaveHelper.LoadInt32(loadBytes, ref index);

			if (version >= 8)
            {
				Magic = SaveHelper.LoadInt32(loadBytes, ref index);
				MaxMagic = SaveHelper.LoadInt32(loadBytes, ref index);
            }

			inventory = Inventory.Load(loadBytes, ref index);

			if (version >= 6)
			{
				accessoryInventory = Inventory.Load(loadBytes, ref index);
				//version 6 uses an inventory with 3 slots, 7 uses 6 slots; it must be expanded.
				if (version == 6)
					accessoryInventory = new Inventory(accessoryInventory, 6);	//expand to be 6 slots
			}

			if (version >= 9)
				gearInventory = Inventory.Load(loadBytes, ref index);

			/*menuPlayer = new MenuPlayer(world.GameStateManager, this, inventory, craftInventory, accessoryInventory, gearInventory);
			menuPlayer.Close();*/
			//currentUI = menuPlayer;

			loadedTimeOfDay = SaveHelper.LoadFloat32(loadBytes, ref index);
            SpawnPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
		}
    }
}
