using A1r.Input;
using BepuPhysics;
using BepuPhysics.Collidables;
using BrUtility;
using Engine;
using Engine.Common;
using Engine.Entities;
using Engine.Items;
using Engine.Networking;
using Engine.Networking.Messages;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.IMGUIImpl;
using ViMG.Items;
using ViMG.Physics;
using ViMG.Rendering;
using ViMG.UIs;
using ViMG.VertexDeclarations;

namespace ViMG
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.AllWithServer)]
	[EntityMeta(19, 0)]
	public class Player : Entity, IHitboxOwner, ISyncBasicState, IRotatable, IHasInventory
	{
        private struct HitboxToSpawnLater
        {
            public int inventorySlot;
            public int damage;
            public DamageType damageType;
            public Vector3 direction;
            public float knockback;
            public float hitboxSize;
            public Buff.BuffInstance[] applyBuffs;
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
			public float MeleeAtkFlat;		//flat damage added on top of scale value. Added AFTER - unmodified by scale.
			public float RangeAtkFlat;
			public float MagicAtkFlat;
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
					MeleeAtkFlat = a.MeleeAtkFlat + b.MeleeAtkFlat,
					RangeAtkFlat = a.RangeAtkFlat + b.RangeAtkFlat,
					MagicAtkFlat = a.MagicAtkFlat + b.MagicAtkFlat,
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

		public struct PlayerExtraState
		{
            public InventoryManager.InventoryReference inventory;
            public InventoryManager.InventoryReference heldInventory;
            public InventoryManager.InventoryReference craftInventory;
            public InventoryManager.InventoryReference gearInventory;
            public InventoryManager.InventoryReference accessoryInventory;
			public int highlightIndex;
			public int useAnimType;
			public float useAnimTime;
			public float useAnimTimer;

			public int currency;

			public float deadTime;
			public float damageTime;
			public float inputLockupTimer;

			public int maxHealth;
			public int maxMagic;
            public int magic;

			public float accel;
			public float speed;
			public float runSpeed;
        }

        public const float INTERACT_DISTANCE = Cube.CUBE_SCALE * 4.5f;

		public enum State
		{
			Noclip,
			Normal,
			Swimming,
			Attack,
			Dash,
			Hurt,
			Dead,
		}

		public static Vector3 BODY_OFFSET = new Vector3(0, Cube.CUBE_SCALE * 0.6f, 0);

        public CubePosition SpawnPosition;
		private float loadedTimeOfDay = -1;

		//public Vector3 Rotation { get; set; }
		public Quaternion Rotation { get; set; }
		public Vector3 Facing;	//The direction the player is facing.

		public static float MaxFallVelocity;
		private float fallStartY;   //the upper-most point of the current jump. If the player hits something > FALL_HEIGHT_FATAL, they will die.
		private const float FALL_HEIGHT_DAMAGE_START = Cube.CUBE_SCALE * 5;
		private const float FALL_HEIGHT_FATAL = Cube.CUBE_SCALE * 18;

		private int currentJumps;
		public float JumpSpeed = 10f * Cube.CUBE_SCALE;
		public bool IsRunning;

		private MouseState currentMS;
		private MouseState previousMS;
		private Vector2 previousMousePosition;

		public State state = State.Normal;
        
		private int oldHighlight = -1;

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
		private float damageTime;
		public const float DAMAGE_ANIM_TIME = 15f / 60f;

		private int hitbox = -1;
		
		private HitboxToSpawnLater hitboxToSpawnLater;
		private DamageType hitboxDamageType = DamageType.Unspecified;
		private Vector3 hitboxOffset;
		private float hitboxTimer;
		private float hitboxSize;
		private const float HITBOX_TIME = 3f / Main.FIXED_FPS;
		private float attackStateTimer;
		private float attackStateMoveTimer;
		private int attackStateInitiatedWeapon; //the weapon that initiated the attack state.

        private float preUseTimer;
		public float useTimer;
		private float useAnimTimer;
		private UseAnimationType useAnimType;
		private ActionStats currentActionStats;
		private const float ATTACK_TIME = 8f / 60f;

		private int numDashes;
		private float dashResetTimer;
		private const float DASH_RESET_TIME = 0.5f;
		private int dashSubstate;
		private float dashTimer;
		private float dashTime;
		private Vector3 dashDirection;
		private float dashDoublePressTimer;
		private const float DOUBLEPRESS_DURATION = 1f / 4f;

		private float deadTime;
		public const float DEAD_TIME = 3f;

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
		private float desiredThirdPersonDistance;
		private float currentThirdPersonDistance;
		private const float THIRDPERSON_MAX_DISTANCE = Cube.CUBE_SCALE * 4f;
		private const float THIRDPERSON_FADEOUT_START = Cube.CUBE_SCALE * 0.9f;
		private const float THIRDPERSON_FADEOUT_END = Cube.CUBE_SCALE * 1.5f;

		private float alive = 0;

		private const float PULL_RADIUS = 3.25f * Cube.CUBE_SCALE;
		private const float NEUTRAL_RADIUS = 2.5f * Cube.CUBE_SCALE;
		private const float PUSH_RADIUS = 1.75f * Cube.CUBE_SCALE;
		private Vector3 attackStateTargetPos;

		private VerySimpleMesh mesh;
		private VerySimpleMesh lookAtMesh;

		private RendererDeferred.DrawMaterial material;
		private RendererDeferred.DrawMaterial lookAtMaterial;

		public const int INVENTORY_ROWS = 4;
		public const int INVENTORY_COLUMNS = 8;

        public InventoryManager.InventoryReference inventory;
        public InventoryManager.InventoryReference heldInventory;
        public InventoryManager.InventoryReference craftInventory;
        public InventoryManager.InventoryReference gearInventory;
        public InventoryManager.InventoryReference accessoryInventory;

        public int Currency;	//we store currency as a flat integer value instead of as items
		//private Menu currentUI;
		//public MenuPlayer menuPlayer;
		// The item currently selected in the main inventory bar. Different from the "held item", which is the item
		// the player has held in hand after they click an item with the inventory open.
		public int highlightIndex;

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

		public int playerUuid;
		public int playerIndex;
		public bool IsLocalPlayer =>
            Main.gameStateManager.netMode == GameStates.GameStateManager.NetworkingMode.Singleplayer || playerIndex == world.localPlayerIndex;

		public bool IsInControl => inputLockupTimer <= 0;

		public double TimeSinceInputSynced;

		public PlayerMovement CurrMovement;
		public PlayerMovement PrevMovement;

		private bool isNew = false;

		public Player() : this(0, new())
		{

		}

        public Player(int playerIndex, int uuid, bool isNew = false)
		{
			this.playerIndex = playerIndex;
			this.playerUuid = uuid;
			this.isNew = isNew;

			SyncInterval = 1;
			AlwaysRender = true;

			//TODO serialize this maybe?
			buffManager = new BuffManagerPlayer(this);
        }

        //Creates a new player from a dead player.
        public Player(Player deadPlayer)
		{
			playerIndex = deadPlayer.playerIndex;
			playerUuid = deadPlayer.playerUuid;

            SyncInterval = 1;
            AlwaysRender = true;

			//TODO serialize this maybe?
			buffManager = new BuffManagerPlayer(this);

			Position = deadPlayer.SpawnPosition.InWorldSpace();

			heldInventory = deadPlayer.heldInventory;
			inventory = deadPlayer.inventory;
			accessoryInventory = deadPlayer.accessoryInventory;
			gearInventory = deadPlayer.gearInventory;
			craftInventory = deadPlayer.craftInventory;
            Currency = deadPlayer.Currency;

            SpawnPosition = CubePosition.FromWorldSpace(deadPlayer.world.WorldInfo.spawnPosition);

            Health = MaxHealth / 4;
        }

		public void FirstCreated(InventoryManager inventoryManager, WorldInfoIO.WorldInfo worldInfo)
		{
			SpawnPosition = CubePosition.FromWorldSpace(worldInfo.spawnPosition);
            Position = worldInfo.spawnPosition;

            Main.Registry.ModRegistry.AddSpawnInventoryItems(inventoryManager.Get(inventory));
		}

        public override void Initialize(World world)
        {
            base.Initialize(world);

			world.InventoryManager.GetOrAdd(ref this.inventory, new Inventory.InventoryConfig(INVENTORY_COLUMNS * INVENTORY_ROWS));
			world.InventoryManager.GetOrAdd(ref heldInventory, new Inventory.InventoryConfig(1 * 1));
            
			MenuHelper.IWhiteList[] whitelistsAccessory = new MenuHelper.IWhiteList[6];
            int[] maxStackSizesAccessory = new int[6];
            Array.Fill(maxStackSizesAccessory, 1);
            for (int i = 0; i < 6; i++)
                whitelistsAccessory[i] = new MenuHelper.WhitelistAccessories(MenuPlayer.tagsAccessoriesBySlot[i]);
			world.InventoryManager.GetOrAdd(ref accessoryInventory, new Inventory.InventoryConfig(6, whitelistsAccessory, maxStackSizesAccessory));

            MenuHelper.IWhiteList[] whitelistsGear = new MenuHelper.IWhiteList[10];
            int[] maxStackSizesGear = new int[10];
            Array.Fill(maxStackSizesGear, 1);
            for (int i = 0; i < MenuPlayer.tagsGearBySlot.Length; i++)
                whitelistsGear[i] = new MenuHelper.WhitelistTag(MenuPlayer.tagsGearBySlot[i]);
            //Start with 10 gear slots so we don't have to worry about expanding in the future.
            //For now, we only have 3:
            //Heart, boots, and feather artefact.
            world.InventoryManager.GetOrAdd(ref gearInventory, new Inventory.InventoryConfig(10, whitelistsGear, maxStackSizesGear));

            world.InventoryManager.GetOrAdd(ref craftInventory, new Inventory.InventoryConfig(8));

            if (isNew)
			{
				FirstCreated(world.InventoryManager, world.WorldInfo);
				isNew = false;
			}

			Console.WriteLine("{0} UUid: {1}", playerIndex, playerUuid);

			//IMGUIConsole.Assert(world.player[playerIndex] == null || world.player[playerIndex].Dead);
			world.player[playerIndex] = this;
            invulnTimer = 6f;   //6 seconds of invuln after respawning

			var inventory = world.InventoryManager.Get(this.inventory)!;
			//if any coins are in the player's inventory, convert them into currency value.
			for (int i = 0; i < INVENTORY_COLUMNS * INVENTORY_ROWS; i++)
			{
				if (inventory.Get(i).item is ItemCoin coin)
				{
					Currency += coin.Value * inventory.Get(i).num;
					inventory.Remove(i, -1);
				}
			}

			hurtbox = -1;
			hitbox = -1;
			Health = MaxHealth;
			Magic = MaxMagic;

            CurrMovement = new PlayerMovement(world.EntityManager.GetReference(this), playerIndex, false);

			(physicsHandle, physicsShapeIndex) = CurrMovement.MakeBody(Position, world.PhysicsInfo);
			contactChecker = new ContactChecker();
   //         var physicsShape = new Capsule(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE * 0.98f);
			//physicsShapeIndex = world.PhysicsInfo.Simulation.Shapes.Add(physicsShape);
			//physicsHandle = world.PhysicsInfo.Simulation.Bodies.Add(BodyDescription.CreateDynamic(
			//	new RigidPose(Position.ToNumerics()), new BodyInertia() { InverseMass = 1f / 20f }, physicsShapeIndex, 0.001f));

			//world.PhysicsInfo.Properties[physicsHandle] = new PhysicsProperties(new SubgroupCollisionFilter(FilterGroups.GROUP_PLAYER, 0), 1f);

			Console.WriteLine("Local id: {0} our id: {1}", world.localPlayerIndex, playerIndex);


			//If we loaded the time of day, set the world's time of day to it.
			if (loadedTimeOfDay > 0)
			{
				world.SetTime(loadedTimeOfDay);
				loadedTimeOfDay = -1;
			}

			//SpawnPosition got corrupted or something or is a version that doesn't have it
			if (SpawnPosition == new CubePosition())
			{
				SpawnPosition = world.ChunkManager.CubeView.GetFirstSolidDown(new CubePosition(world.sizeInCubes / 2, world.sizeInCubes, world.sizeInCubes / 2)).GetOrDefault(new CubePosition());
			}
		}

        public override void OnKill()
        {
            base.OnKill();

			if (state == State.Dead)
			{
				// TODO drop items
				// this never worked to begin with...
				//for (int i = 0; i < craftInventory.NumSlots; i++)
				//{
				//	if (craftInventory.Get(i).valid)
				//	{
				//		EntityItem ent = new EntityItem(Position, new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5), Cube.CUBE_SCALE * 1.6f,
				//			Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5)), craftInventory.Get(i));
				//		world.EntityManager.Add(ent);
				//	}
				//}

				//TODO death screen and stuff
				world.PlayerRespawnedEvent.Add(this);
				world.player[playerIndex] = null;
            }
		}

        public override void OnUnload()
        {
            base.OnUnload();

			world.InventoryManager.Unload(inventory);
            world.InventoryManager.Unload(heldInventory);
            world.InventoryManager.Unload(gearInventory);
            world.InventoryManager.Unload(accessoryInventory);

            // Player should only ever be unloaded/removed in two scenarios:
            // The world is being disposed (we're exiting the game),
            // or a client disconnected from the server.
            // Even if the player is off in the middle of nowhere in some unloaded area of the game (as might be the case with other clients),
            // it should stay loaded.
            // TODO: revisit this. Maybe not the best way of doing things. It's possible we COULD allow players to be unloaded so long as they're
            // not the local player.
            //IMGUIConsole.Assert(world.isCreateWorldReloading || world.isDisposed || Main.gameStateManager.TheIsland.netManager.netPlayers[playerIndex].playerId == -1);

            if (hitbox != -1)
				world.HitboxManager.Remove(hitbox);
			if (hurtbox != -1)
				world.HitboxManager.Remove(hurtbox);
			hitbox = -1;
			hurtbox = -1;

			world.PhysicsInfo.Simulation.Bodies.Remove(physicsHandle);
			world.PhysicsInfo.Simulation.Shapes.Remove(physicsShapeIndex);

			physicsHandle = new BodyHandle();
			physicsShapeIndex = new TypedIndex();

			world.player[playerIndex] = null;
		}

        public override void Update(double deltaTime)
		{
			Get(out var get);
			CurrMovement.Update(ref get, deltaTime);
			//Position = get.position;
			world.PhysicsInfo.Simulation.Bodies[physicsHandle].Velocity = get.velocity.ToNumerics();

            if (state != State.Noclip)
				Position = world.PhysicsInfo.Simulation.Bodies[physicsHandle].Pose.Position + BODY_OFFSET;
			else
			{
				world.PhysicsInfo.Simulation.Bodies[physicsHandle].Pose.Position = get.position.ToNumerics();
				Position = get.position;
            }

			hasMoved = false;
			hasRotated = false;

			var inventory = world.InventoryManager.Get(this.inventory);
			var craftInventory = world.InventoryManager.Get(this.craftInventory);
            var gearInventory = world.InventoryManager.Get(this.gearInventory);
            var accessoryInventory = world.InventoryManager.Get(this.accessoryInventory);
            var heldInventory = world.InventoryManager.Get(this.heldInventory);
			inventory?.ProcessEventsServer(this);
			craftInventory?.ProcessEventsServer(this);
			gearInventory?.ProcessEventsServer(this);
			accessoryInventory?.ProcessEventsServer(this);
			heldInventory?.ProcessEventsServer(this);

			if (IsLocalPlayer)
			{
				if (Main.inputManager.JustPressed(Keys.G))
				{
					if (state == State.Noclip)
						state = State.Normal;
					else state = State.Noclip;
				}
			}
			else
			{
				// Check to make sure we're still alive
				// This is the case if our playerIndex is present in the netPlayer array
				if (Main.gameStateManager.TheIsland.netManagerServer?.netPlayers[playerIndex].playerId != playerIndex)
				{
					world.EntityManager.Kill(this);
					return;
				}
			}

			var bh = world.PhysicsInfo.Simulation.Bodies[physicsHandle];
			if (state != State.Noclip && (!world.ChunkManager.IsInWorldBounds(Position) ||
				!world.ChunkLoadManager.IsLoaded(ChunkPosition.WorldSpaceChunk(Position))))
			{
				if (bh.Awake)
					bh.Awake = false;
			}
			else if (!bh.Awake)
				bh.Awake = true; //player physics shape can never fall asleep

			if (hurtbox == -1)
				hurtbox = world.HitboxManager.Add(this, Bounds, Vector3.Zero, HitboxManager.Group.PLAYER_TAKE, -1, -1f, invulnTimer <= 0);
			else if (state != State.Noclip)
				world.HitboxManager.Update(hurtbox, Bounds.ToOBB(), invulnTimer <= 0);

			UpdateStats(deltaTime);

			if (inputLockupTimer <= 0)
			{
				healthRegenTimer -= (float)deltaTime;
				magicRegenTimer -= (float)deltaTime;

				if (healthRegenTimer <= 0)
                {
					healthRegenTimer = stats.HPRegenTime;
					Heal(stats.HPRegenAmt);	//minimum of 0 hpregen; we will never regen unless stats have been increased
                }

				if (magicRegenTimer <= 0)
                {
					magicRegenTimer = 10 - stats.MPRegenTime;

					Magic += stats.MPRegenAmt + 1;	//minimum of 1 mana regen; we will always regen even if stats are unaffected

					if (Magic > GetCalculatedMaxMagic())
						Magic = GetCalculatedMaxMagic();
                }
			} 
			else
			{
				inputLockupTimer -= (float)deltaTime;
			}

			if (state == State.Noclip)
			{
				UpdateMovementNoclip(deltaTime);
			}
			else if (state == State.Dead)
			{
				if ((float)Main.Time - deadTime > DEAD_TIME)
					KillWithoutAnimation();
			}
			else if (state == State.Normal)
			{
				if (Main.inputManager.JustPressed(Keys.LeftControl))
					ExpandedMineState = !ExpandedMineState;

				invulnTimer -= (float)deltaTime;

				UpdateCollisionType();

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
				if (inputLockupTimer <= 0)
					state = State.Normal;
			}

            contactChecker.Update(world.PhysicsInfo, physicsHandle);

			if (world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear.Length() > float.Epsilon)
				hasMoved = true;

			int scroll = Main.inputManager.GetMouseScroll();

            if (scroll != 0)
			{
				int sign = Math.Sign(scroll);

				desiredThirdPersonDistance += sign;

				desiredThirdPersonDistance = float.Clamp(desiredThirdPersonDistance, 0, THIRDPERSON_MAX_DISTANCE);
			}

			if (Main.inputManager.JustPressed(Keys.PageDown))
			{
				desiredThirdPersonDistance += Cube.CUBE_SCALE;

                desiredThirdPersonDistance = float.Clamp(desiredThirdPersonDistance, 0, THIRDPERSON_MAX_DISTANCE);
            }
			if (Main.inputManager.JustPressed(Keys.PageUp))
			{
                desiredThirdPersonDistance -= Cube.CUBE_SCALE;

                desiredThirdPersonDistance = float.Clamp(desiredThirdPersonDistance, 0, THIRDPERSON_MAX_DISTANCE);
            }

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
					world.HitboxManager.Update(hitbox, new Engine.Physics.OrientedBoundingBox(Position + hitboxOffset, new Vector3(hitboxSize / 2f), Quaternion.Identity));
				}
			}

			if (IsLocalPlayer)
			{
				if (Main.inputManager.JustPressed(Keys.F3))
				{
					Main.DebugChunks = !Main.DebugChunks;
				}

				//if (Main.inputManager.JustPressed(Keys.E))
				//{
				//	if (Main.gameStateManager.GetCurrentGameState().GetCurrentMenu() != menuPlayer)
				//		Main.gameStateManager.GetCurrentGameState().PopMenu();
				//	else menuPlayer.Toggle();
				//}

				if (useTimer <= 0)
				{
					if (oldHighlight != highlightIndex)
					{
						if (oldHighlight != -1 && inventory.Get(oldHighlight).valid)
						{
							inventory.Get(oldHighlight).item?.EndHold(this, inventory, highlightIndex);

							if (inventory.Get(highlightIndex).valid)
								inventory.Get(highlightIndex).item?.StartHold(this, inventory, highlightIndex);
						}

                        if (inventory.Get(highlightIndex).valid)
                            inventory.Get(highlightIndex).item?.StartHold(this, inventory, highlightIndex);
                    }
					oldHighlight = highlightIndex;
				}
			}

			/*if (Main.inputManager.JustPressed(Keys.Escape))
            {
				world.GameStateManager.GetCurrentGameState().PushMenu(new MenuPause(world.GameStateManager, world));
			}*/

			if (inventory.Get(highlightIndex).valid)
				inventory.Get(highlightIndex).item.Hold(this, inventory, highlightIndex);

			var fwd = (this as IRotatable).Forward;

           lookAtResult = CubeView.Raycast(Position, Position - fwd * INTERACT_DISTANCE, CubeView.RaycastCallbackTouchable, world.ChunkManager.CubeView);

			IsLooking = false;
			CanPlace = false;
			if (lookAtResult.hasHit)
			{
				if (world.ChunkManager.IsInWorldBounds(lookAtResult.hit))
				{
					var c = world.ChunkManager.CubeView.GetCube(CubePosition.FromWorldSpace(lookAtResult.hit));
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

			UpdateThrowItem();

            hitboxTimer -= (float)deltaTime;

			if (preUseTimer <= 0)
			{
				if (hitboxToSpawnLater.damage > 0)
				{
					SpawnHitbox(hitboxToSpawnLater);

					hitboxToSpawnLater = new HitboxToSpawnLater();
				}

				if (useTimer > 0)
					useTimer -= (float)deltaTime;
				else useTimer = 0;
				if (useAnimTimer > 0)
					useAnimTimer -= (float)deltaTime;
				else useAnimTimer = 0;

				if (useAnimTimer <= 0 && useTimer <= 0)
					useAnimType = UseAnimationType.None;
			}
			else preUseTimer -= (float)deltaTime;

			alive += (float)deltaTime;

            PrevMovement = CurrMovement;
        }

        private void UpdateCollisionType()
		{
			inWater = false;
			inRope = false;

			Cube cube = world.ChunkManager.CubeView.GetCube(CubePosition.FromWorldSpace(Position)).GetOrDefault(Main.Registry.CubeRegistry.Air);
            
			if (cube.Collision == Cube.CollisionValue.LiquidWater)
				inWater = true;
		}

		private IDashEffect dashEffect = new DefaultDashEffect();
		private IJumpEffect[] jumpEffects = new IJumpEffect[4];
		private void UpdateStats(double deltaTime)
		{
			var accessoryInventory = world.InventoryManager.Get(this.accessoryInventory);
            var gearInventory = world.InventoryManager.Get(this.gearInventory);

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
			Vector3 velocity = world.PhysicsInfo.Simulation.Bodies[physicsHandle].Velocity.Linear;

            if ((contactChecker.OnGround || currentJumps > 0) && CurrMovement.Jump.JustPressed(PrevMovement.Jump))
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

			if (velocity.Length() > float.Epsilon)
				hasMoved = true;

			world.PhysicsInfo.Simulation.Bodies[physicsHandle].Dynamics.Motion.Velocity.Linear = velocity.ToNumerics();

			UpdatePerformAction();
		}

		private void UpdateMovementNoclip(double deltaTime)
		{
            world.PhysicsInfo.Simulation.Bodies[physicsHandle].Pose.Position = Position.ToNumerics();
            world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear = Vector3.Zero.ToNumerics();

            Vector3 oldPos = Position;

            if (Position != oldPos)
                hasMoved = true;

            UpdatePerformAction();

			invulnTimer = INVULN_TIME;
            fallStartY = Position.Y;    //so we don't immediately die sometimes
        }

		private float DEBUGTimeSkipHeldTime;

		private void UpdateMovement(double deltaTime)
		{
			bool movementPressed = false;
			//Vector2 velXY = new Vector2(Velocity.X, Velocity.Z);
			Vector3 velocity = world.PhysicsInfo.Simulation.Bodies[physicsHandle].Dynamics.Motion.Velocity.Linear;

			if (IsInControl)
			{
				if ((contactChecker.OnGround || currentJumps > 0) && CurrMovement.Jump.JustPressed(PrevMovement.Jump))
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

				UpdatePerformAction();
			}

			if (movementPressed || velocity.Length() > float.Epsilon)
			{
				hasMoved = true;
				Facing = Vector3.Normalize(velocity);
			}

			if (velocity.Y > Cube.CUBE_SCALE * 3.2f && !CurrMovement.Jump.Pressed())
			{
				velocity.Y = Cube.CUBE_SCALE * 3.2f;
			}

			world.PhysicsInfo.Simulation.Bodies[physicsHandle].Dynamics.Motion.Velocity.Linear = velocity.ToNumerics();

			//UpdateMaybeDash(deltaTime);

			DEBUGTimeSkipHeldTime += (float)deltaTime;

			if (Main.inputManager.JustPressed(Keys.T))
			{
				DEBUGTimeSkipHeldTime = 0;
				world.TimeScale = 2f;
			}

			if (Main.inputManager.JustReleased(Keys.T))
			{
				world.TimeScale = 1f;

				if (DEBUGTimeSkipHeldTime <= 0.25f)
					world.AddTime(World.DAY_CYCLE_TIME * 0.25f);

				DEBUGTimeSkipHeldTime = 0;
			}
		}

		private void UpdatePerformAction()
		{
			var inventory = world.InventoryManager.Get(this.inventory);

			if (IsInControl && useTimer <= 0)
            {
				if (CurrMovement.LeftClick.Pressed())
                {
                    if (inventory.Get(highlightIndex).item != null && 
						inventory.Get(highlightIndex).item.LeftClick(this, inventory, highlightIndex, 
						-(this as IRotatable).Forward, out ActionStats actionStats))
                        PerformAction(actionStats);
					else
					{
                        Cube cube = world.ChunkManager.CubeView.GetCube(LookAtPos).GetOrDefault(Main.Registry.CubeRegistry.Air);
                        cube.OnLeftClick(world, LookAtPos);
						SyncCubeAction.Instance.QueueAction(new SyncCubeAction.CubeAction
						{
							cube = cube,
							leftClicked = true,
							playerId = playerIndex,
							position = LookAtPos,
						});

						PerformAction(new ActionStats(Item.DEFAULT_USE_TIME));
                    }
                }

                if (CurrMovement.RightClick.Pressed())
                {
                    bool performedAction = false;
                    var entityTracking = world.EntityManager.GetEntityTrackingPosition(LookAtPos).GetOrDefault(null);

                    if (entityTracking != null)
                    {
                        if (entityTracking is ICubeTracker tracker)
                        {
                            if (tracker.OnInteract(this))
                            {
                                PerformAction(new ActionStats(Item.DEFAULT_USE_TIME));
                                performedAction = true;
                            }
                        }
                        else if (entityTracking is IMultiCubeTracker multiTracker)
                        {
                            if (multiTracker.OnInteract(this))
                            {
                                PerformAction(new ActionStats(Item.DEFAULT_USE_TIME));
                                performedAction = true;
                            }
                        }
                    }

					if (!performedAction && inventory.Get(highlightIndex).item != null && inventory.Get(highlightIndex).item
						.RightClick(this, inventory, highlightIndex, -(this as IRotatable).Forward, out ActionStats actionStats))
					{
						PerformAction(actionStats);
						performedAction = true;
					}

					if (!performedAction)
					{
						Cube cube = world.ChunkManager.CubeView.GetCube(LookAtPos).GetOrDefault(Main.Registry.CubeRegistry.Air);

						if (cube.CanRightClick(LookAtPos))
						{
							cube.OnRightClick(world, LookAtPos);
                            SyncCubeAction.Instance.QueueAction(new SyncCubeAction.CubeAction
                            {
                                cube = cube,
                                rightClicked = true,
                                playerId = playerIndex,
                                position = LookAtPos,
                            });

                            PerformAction(new ActionStats(Item.DEFAULT_USE_TIME));
						}
					}
                }
            }
        }

		//private void UpdateMaybeDash(double deltaTime)
  //      {
		//	if (contactChecker.OnGround)
  //          {
		//		dashResetTimer -= (float)deltaTime;
  //          }

		//	if (dashResetTimer <= 0)
		//	{
		//		if (stats.DashEffect != null && stats.DashNum > 0)
		//		{
		//			dashDoublePressTimer -= (float)deltaTime;

		//			if (dashSubstate == 0)
		//			{
		//				if (Run.JustPressed(prevRun))
		//				{
		//					dashSubstate++;
		//					dashDoublePressTimer = DOUBLEPRESS_DURATION;
		//				}
		//			}
		//			else if (dashSubstate == 1)
		//			{
		//				if (dashDoublePressTimer >= 0)
		//				{
		//					if (Run.JustPressed(prevRun))
		//					{
		//						state = State.Dash;

		//						Vector3 vel = world.PhysicsInfo.Simulation.Bodies[physicsHandle].Dynamics.Motion.Velocity.Linear;
		//						stats.DashEffect.StartDash(this, ref vel, out dashDirection, out dashTime, in stats);
		//						world.PhysicsInfo.Simulation.Bodies[physicsHandle].Dynamics.Motion.Velocity.Linear = vel.ToNumerics();
		//						dashTimer = dashTime;

		//						dashSubstate = 0;
		//					}
		//				}
		//				else
		//				{
		//					dashSubstate = 0;
		//				}
		//			}
		//		}
		//	}
  //      }

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
			if (inventory.Get(index).valid)
			{
				// TODO Use rotation
				ItemInstance thrownInstance = new ItemInstance(inventory.Get(index), num);
				EntityItem ent = new EntityItem(Position, -(this as IRotatable).Forward * Cube.CUBE_SCALE * 5, thrownInstance);
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

            var inventory = world.InventoryManager.Get(this.inventory);

            var items = world.EntityManager.GetAll<EntityItem>();
			if (items != null)
			{
				foreach (var ent in items)
				{
					EntityItem item = ent as EntityItem;

					if (!inventory.CanAdd(item.ItemInstance.item))
						continue;

					Vector3 dir = Position - ent.Position;

					if (item.CanBePickedUp && dir.Length() < pickupRadius)
					{
						//coins are handled manually due to the fact that they should add themselves to the player currency value
						//instead of to the inventory.
						if (item.ItemInstance.item is ItemCoin coin)
						{
                            world.EntityManager.Kill(ent);

                            Currency += item.ItemInstance.num * coin.Value;

							// TODO: how to convey this to client?
							//if (IsLocalPlayer)
							//	menuPlayer.AddPickedUpItem(item.ItemInstance);
                        }
						else
						{
							if (inventory.Add(item.ItemInstance, out int index))
							{
								world.EntityManager.Kill(ent);
								item.ItemInstance.item.StartHold(this, inventory, index);

                                // TODO: how to convey this to client?
         //                       if (IsLocalPlayer)
									//menuPlayer.AddPickedUpItem(item.ItemInstance);
							}
						}
					}
					else if (item.CanBePickedUp && dir.Length() < suckRadius)
					{
						item.MoveTowards(Position);
					}
				}
			}
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

					int damage = (int)((float)GetCalculatedMaxHealth() * t);

					Damage(damage);
				}

				fallStartY = Position.Y;
			}
		}

		private void UpdateThrowItem()
		{
            var inventory = world.InventoryManager.Get(this.inventory);

            if (CurrMovement.Throw.JustPressed(PrevMovement.Throw))
            {
                int inventorySlot = highlightIndex;

                if (inventory.Get(inventorySlot).valid)
                {
                    int num = 1;
                    if (Main.inputManager.IsPressed(Keys.LeftControl))
                        num = inventory.Get(inventorySlot).num;

                    ThrowItem(inventory, inventorySlot, num);
                }
            }
        }

		public void PerformAction(ActionStats actionStats)
		{
			this.useTimer = actionStats.useTime;
			this.useAnimTimer = actionStats.useAnimTime;

			this.preUseTimer = actionStats.preUseTime;

			this.useAnimType = actionStats.animationType;

			currentActionStats = actionStats;
		}

		public void PerformAttack(DamageType damageType, ref ActionStats actionStats, ref int damage, ref float knockback)
		{
            var inventory = world.InventoryManager.Get(this.inventory);
            var accessoryInventory = world.InventoryManager.Get(this.accessoryInventory);

            float speedScale = 0;

			this.hitboxDamageType = DamageType.Unspecified;

			if (damageType == DamageType.Melee)
				speedScale = stats.MeleeSpdScale;
			else if (damageType == DamageType.Ranged)
				speedScale = stats.RangeSpdScale;
			else if (damageType == DamageType.Magic)
				speedScale = stats.MagicSpdScale;

			damage = DealDamageCalculation(damageType, damage);

			for (int i = 0; i < accessoryInventory.NumSlots; i++)
            {
				if (accessoryInventory.Get(i).valid)
					accessoryInventory.Get(i).item.OnAttack(this, inventory, highlightIndex);
            }

			actionStats.useTime -= (actionStats.useTime * speedScale);
			actionStats.useAnimTime -= (actionStats.useAnimTime * speedScale);
			this.attackStateTimer = actionStats.useTime + actionStats.preUseTime;
			this.attackStateMoveTimer = 1f / Main.FIXED_FPS;

			state = State.Attack;
		}

		/*public void SpawnHitbox(int inventorySlot, int damage, DamageType damageType, Vector3 direction, 
			float knockback = 1, float hitboxSize = Cube.CUBE_SCALE * 1.75f, Buff.BuffInstance[] applyBuffs = null)
		{
			if (hitbox != -1)
				world.HitboxManager.Remove(hitbox);
			this.hitboxDamageType = damageType;

			float offset = hitboxSize + (Cube.CUBE_SCALE / 2f) - (hitboxSize / 2f);
			hitboxOffset = direction * offset;

			Rectangle3D rect = new Rectangle3D(Position + hitboxOffset, new Vector3(hitboxSize));
			this.hitboxSize = hitboxSize;

			hitbox = world.HitboxManager.Add(this, rect, -Main.camera.Forward, HitboxManager.Group.PLAYER_DEAL, 
				DealDamageCalculation(damageType, damage), knockback, 
				applyBuffs: applyBuffs, inventorySlot: inventorySlot);

			hitboxTimer = HITBOX_TIME;
		}*/

		public void SpawnHitboxLater(int inventorySlot, int damage, DamageType damageType, Vector3 direction,
            float knockback = 1, float hitboxSize = Cube.CUBE_SCALE * 1.75f, Buff.BuffInstance[] applyBuffs = null)
		{
			hitboxToSpawnLater = new HitboxToSpawnLater()
			{
				inventorySlot = inventorySlot,
				damage = damage,
				damageType = damageType,
				direction = direction,
				knockback = knockback,
				hitboxSize = hitboxSize,
				applyBuffs = applyBuffs
			};
		}

		private void SpawnHitbox(HitboxToSpawnLater toSpawnLater)
		{
            if (hitbox != -1)
                world.HitboxManager.Remove(hitbox);
            this.hitboxDamageType = toSpawnLater.damageType;

            float offset = hitboxSize + (Cube.CUBE_SCALE / 2f) - (hitboxSize / 2f);
            hitboxOffset = toSpawnLater.direction * offset;

            this.hitboxSize = toSpawnLater.hitboxSize;
			var quat = EngineMathHelper.LookRotation(toSpawnLater.direction, Vector3.Up);
			var obb = new Engine.Physics.OrientedBoundingBox(Position + hitboxOffset, new(hitboxSize / 2f), quat);
            // TODO use rotation
            world.HitboxManager.Add(new HitboxManager.HitboxParameters
			{
				owner = this,
				manager = null,
				bounds = obb,
				direction = toSpawnLater.direction,
				stats = new HitboxManager.HitboxStats
				{
					damage = DealDamageCalculation(toSpawnLater.damageType, toSpawnLater.damage),
					group = HitboxManager.Group.PLAYER_DEAL,
					knockback = toSpawnLater.knockback,
					applyBuffs = toSpawnLater.applyBuffs,
					inventorySlot = toSpawnLater.inventorySlot,
					expirationTime = (float)Main.Time + HITBOX_TIME,
				},
			});
            //hitbox = world.HitboxManager.Add(this, rect, -(this as IRotatable).Forward, HitboxManager.Group.PLAYER_DEAL,
            //    DealDamageCalculation(toSpawnLater.damageType, toSpawnLater.damage), toSpawnLater.knockback,
            //    applyBuffs: toSpawnLater.applyBuffs, inventorySlot: toSpawnLater.inventorySlot);

            hitboxTimer = HITBOX_TIME;
        }

		public override void Draw(GraphicsDevice device, Effect effect)
		{
   //         var inventory = world.InventoryManager.Get(this.inventory);

   //         lookAtMaterial = StaticMaterials.Cubes;

   ////         if (inventory.Get(highlightIndex).item != null)
			////{
			////	// TODO use Rotation
			////	inventory.Get(highlightIndex).item.DrawInHand(device, inventory.Get(highlightIndex), this, -(this as IRotatable).Forward);
			////}

			//if (mesh.IBO == null)
			//	mesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 0.98f * 2f, Enums.Alignment.Center);
			//	//mesh = MeshHelper.MakeCenteredQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 0.98f * 2f);

			//if (lookAtMesh.IBO == null)
			//{
   //             FastList<VertexCube> vertices = new FastList<VertexCube>();
   //             List<int> indices = new List<int>();
			//	MeshHelper.MakeCubeVertsVertexPositionColorTextureNormal(Vector3.Zero, new Vector3(Cube.CUBE_SCALE), MeshHelper.CubeFace.ALL, Color.White, vertices, indices);
			//	lookAtMesh = VerySimpleMesh.Transparent(device, ChunkRenderMesher.VertexAttributes.Transparent(vertices, indices));
			//	//lookAtMesh = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexTransparentPass(), indices);//MeshHelper.MakeCubeVertexPositionColorTextureNormal(device, Vector3.Zero, new Vector3(Cube.CUBE_SCALE), MeshHelper.CubeFace.ALL, Color.White, DrawHelper.WhitePixel);
			//	//lookAtMesh = MeshHelper.MakeCubeVertexPositionColor(device, Vector3.Zero, new Vector3(Cube.CUBE_SCALE), MeshHelper.CubeFace.ALL, Color.White, DrawHelper.WhitePixel);
			//	//lookAtMesh.Name = "Look At Mesh";
			//}

			//if (currentThirdPersonDistance > THIRDPERSON_FADEOUT_START)
			//{
			//	float p = ((currentThirdPersonDistance - THIRDPERSON_FADEOUT_START) / 
			//		(THIRDPERSON_FADEOUT_END - THIRDPERSON_FADEOUT_START));

			//	Color color = Color.White * p;

			//	// TODO use local Rotation
			//	Matrix worldMat = Matrix.CreateRotationX(Math.Clamp(-Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
			//		Matrix.CreateRotationY(-Rotation.Y) *
			//		Matrix.CreateTranslation(world.PhysicsInfo.Simulation.Bodies[physicsHandle].Pose.Position);

   //             /*if (currentThirdPersonDistance < THIRDPERSON_FADEOUT_END) 
			//	{
			//		Main.Renderer.DrawsTransparentPass.Add(new Rendering.RendererDeferred.TransparentDraw(currentThirdPersonDistance,
   //                     worldMat, DrawHelper.WhitePixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO, null, color));
			//	}
			//	else
			//	{
			//		Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(DrawHelper.WhitePixel,
			//			DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO, worldMat, null, color.ToVector3()));
			//	}*/
   //         }

			//if (IsLocalPlayer && lookAtResult.hasHit && world.ChunkManager.IsInWorldBounds(lookAtResult.hit))
			//{
			//	float s = MathF.Sin(MathF.PI * 2f * (alive % 2f)) * 0.5f + 0.5f;
			//	Color color = Color.Lerp(Color.White, Color.Black, s);

			//	if (ExpandedMineState && 
			//		inventory.Get(highlightIndex).valid && inventory.Get(highlightIndex).item is IHasAreaEffect pickStats)
			//	{
			//		CubePosition[] positions = pickStats.GetAffectedPositions(world.ChunkManager.CubeView, inventory.Get(highlightIndex), Position, LookAtPos.InWorldSpace(), lookAtResult.normal, out _);
			//		Span<ushort> ids = stackalloc ushort[positions.Length];

			//		world.ChunkManager.CubeView.GetIds(positions.AsSpan(), ids);

			//		for (int i = 0; i < positions.Length; i++)
			//		{
			//			if (pickStats.CanPredictAir() || Main.Registry.CubeRegistry.GetOrDefault(ids[i], Main.Registry.CubeRegistry.Air).Touchable)
			//			{
			//				Main.Renderer.AddTransparentDraw(new Rendering.RendererDeferred.TransparentDraw((int)lookAtResult.end.Length(), lookAtMaterial,
   //                             lookAtMesh,
   //                             Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 2f)) *
			//					Matrix.CreateScale(1.126f) *
			//					Matrix.CreateTranslation(new Vector3(Cube.CUBE_SCALE / 2f)) *
			//					Matrix.CreateTranslation(positions[i].InWorldSpace()),
			//					new RectangleF(0, 1008, 16, 16), color));
			//			}
			//		}
			//	}
			//	else 
			//	{
			//		Main.Renderer.AddTransparentDraw(new Rendering.RendererDeferred.TransparentDraw((int)lookAtResult.end.Length(), lookAtMaterial,
   //                     lookAtMesh,
   //                     Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 2f)) *
			//			Matrix.CreateScale(1.126f) *
			//			Matrix.CreateTranslation(new Vector3(Cube.CUBE_SCALE / 2f)) *
			//			Matrix.CreateTranslation(LookAtPos.InWorldSpace()),
			//			new RectangleF(0, 1008, 16, 16), color));
			//	}
			//}
		}

		public void DrawUI(SpriteBatch batch)
		{
			if (deadTime > 0 && state == State.Dead)
			{
				float t = 1 - (deadTime / DEAD_TIME);

				batch.DrawRectangle(new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y), Color.Black * t);
			}

			if (alive < 0.5f)
			{
				float t = 1 - (alive / 0.5f);

				batch.DrawRectangle(new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y), Color.Black * t);
			}

			if (damageTime >= 0)
			{
				float t = damageTime / DAMAGE_ANIM_TIME;

				batch.DrawRectangle(new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y), Color.DarkRed * t);
			}

			if (headUnderWater)
			{
				batch.DrawRectangle(new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y), Color.Blue * 0.5f);
			}
		}

		public static Matrix GetHeldMatrix(BasicState player, Vector2 origin, Vector3 scale)
		{
			var extraState = player.GetExtra<PlayerExtraState>();
			float percent = extraState.useAnimTimer / extraState.useAnimTime;

			if (percent <= 0 || float.IsNaN(percent))
				percent = 0;

			// TODO use Rotation instead of Forward/Up/LR
			switch ((UseAnimationType)extraState.useAnimType)
			{
				case UseAnimationType.SwingHorizontal:
					{
						float ang = 180 * percent;
                        //Matrix.CreateTranslation(-origin.X, -origin.Y, 0) *
                        //    Matrix.CreateScale(hitboxSize / Cube.CUBE_SCALE) *
                        //    Matrix.CreateRotationX(MathHelper.ToRadians(-90)) *
                        //    Matrix.CreateRotationY(MathHelper.ToRadians(-245 - ang)) *
                        //    Matrix.CreateRotationX(-Main.camera.Rotation.X) *
                        //    Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
                        //    Matrix.CreateTranslation(Position -
                        //    Main.camera.Forward * Cube.CUBE_SCALE / 4f -
                        //    Main.camera.Up * Cube.CUBE_SCALE / 4f);
                        return
							Matrix.CreateTranslation(-origin.X, -origin.Y, 0) *
							Matrix.CreateScale(0.5f * scale) *
							Matrix.CreateRotationX(MathHelper.ToRadians(-90)) *
							Matrix.CreateRotationY(MathHelper.ToRadians(-245 - ang)) *
							Matrix.CreateFromQuaternion(player.rotation) *
							Matrix.CreateTranslation(player.position) *
							Matrix.CreateTranslation(-BasicState.Forward(ref player) * Cube.CUBE_SCALE / 4f) *
							Matrix.CreateTranslation(-BasicState.Up(ref player) * Cube.CUBE_SCALE / 4f);
						// TODO rework
							//Matrix.CreateRotationX(-Rotation.X) *
							//Matrix.CreateRotationY(-Rotation.Y) *
							//Matrix.CreateTranslation(Position -
       //                     (this as IRotatable).Forward * Cube.CUBE_SCALE / 4f -
       //                     (this as IRotatable).Up * Cube.CUBE_SCALE / 4f);
					}
				case UseAnimationType.SwingVertical:
					{
						float ang = 180 * (1 - percent);
						return Matrix.CreateTranslation(-origin.X, -origin.Y, 0) *
							Matrix.CreateScale(Cube.CUBE_SCALE) *
							Matrix.CreateRotationY(MathHelper.ToRadians(-90)) *
							Matrix.CreateRotationX(MathHelper.ToRadians(-ang)) *
							Matrix.CreateFromQuaternion(player.rotation) *
							Matrix.CreateTranslation(player.position);
							//Matrix.CreateRotationX(-Rotation.X) *
							//Matrix.CreateRotationY(-Rotation.Y) *
							//Matrix.CreateTranslation(Position -
							//(this as IRotatable).Forward * Cube.CUBE_SCALE / 2 +
							//(this as IRotatable).Right * Cube.CUBE_SCALE / 4 -
       //                     (this as IRotatable).Up * Cube.CUBE_SCALE / 4);
					}
                case UseAnimationType.Use:
				case UseAnimationType.None:
				default:
					return Matrix.CreateTranslation(-origin.X, -origin.Y, 0) *
						Matrix.CreateScale(0.5f * scale) *
						Matrix.CreateRotationZ(MathHelper.ToRadians(35f) * percent) *
						Matrix.CreateRotationY(MathHelper.ToRadians(-45f)) *
						Matrix.CreateFromQuaternion(player.rotation) *
						Matrix.CreateTranslation(player.position) *
						Matrix.CreateTranslation(-BasicState.Forward(ref player) * Cube.CUBE_SCALE / 3f) *
						Matrix.CreateTranslation(BasicState.Right(ref player) * Cube.CUBE_SCALE / 4f) *
						Matrix.CreateTranslation(-BasicState.Up(ref player) * Cube.CUBE_SCALE / 6f);
			}
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
            var inventory = world.InventoryManager.Get(this.inventory);
            var accessoryInventory = world.InventoryManager.Get(this.accessoryInventory);

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
						Vector3 direction = Vector3.Normalize(Position - other.bounds.Center);
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

			deadTime = world.GetTime();
			state = State.Dead;
        }

		public void KillWithoutAnimation()
        {
			world.EntityManager.Kill(this);
        }

		private void Damage(int amt)
        {
			if (state == State.Noclip)
				return;

			damageTime = world.GetTime();

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
				calculatedDamage += stats.MeleeAtkFlat;
			}
			else if (damageType == DamageType.Magic)
			{
				calculatedDamage *= startScale + stats.MagicAtkScale;
				calculatedDamage += stats.MagicAtkFlat;
			}
			else if (damageType == DamageType.Ranged)
			{
				calculatedDamage *= startScale + stats.RangeAtkScale;
				calculatedDamage += stats.RangeAtkFlat;
			}
			else calculatedDamage *= startScale;

			return (int)calculatedDamage;
        }

		public void Heal(int amt)
        {
			Health += amt;

			if (Health > GetCalculatedMaxHealth())
				Health = GetCalculatedMaxHealth();
        }

		public BuffManagerPlayer GetBuffManager()
		{
			return buffManager;
		}

		public ref AccumulatedStats GetStats()
        {
			return ref stats;
        }

		public int GetCalculatedMaxHealth()
        {
			int hp = MaxHealth;
			hp += (int)((float)hp * stats.HPScale);
			hp += stats.HPFlat;

			return hp;
        }

		public int GetCalculatedMaxMagic()
		{
			int mp = MaxMagic;
			mp += (int)((float)mp * stats.MPScale);
			mp += stats.MPFlat;

			return mp;
		}

		public override void OnSave(List<byte> saveBytes)
		{
			base.OnSave(saveBytes);

			SaveHelper.SaveVector3(saveBytes, Position);
			//SaveHelper.SaveCubePosition(saveBytes, CubePosition.FromWorldSpace(Position));
			SaveHelper.SaveQuaternion(saveBytes, Rotation);

			SaveHelper.SaveInt32(saveBytes, Health);
			SaveHelper.SaveInt32(saveBytes, MaxHealth);
			SaveHelper.SaveInt32(saveBytes, Magic);
			SaveHelper.SaveInt32(saveBytes, MaxMagic);

			world.InventoryManager.Get(inventory)!.Save(saveBytes);
            world.InventoryManager.Get(accessoryInventory)!.Save(saveBytes);
            world.InventoryManager.Get(gearInventory)!.Save(saveBytes);

			SaveHelper.SaveInt32(saveBytes, Currency);

			SaveHelper.SaveFloat32(saveBytes, world?.GetTime() ?? -1);
			SaveHelper.SaveCubePosition(saveBytes, SpawnPosition);

			if (world != null)
				SaveHelper.SaveInt32(saveBytes, 0);
			else SaveHelper.SaveInt32(saveBytes, 0);

			//Get(out BasicState state);
			//state.OnSave(saveBytes);

			SaveHelper.SaveInt32(saveBytes, playerIndex);
			SaveHelper.SaveInt32(saveBytes, playerUuid);
		}

        public override void OnLoad(World world, byte[] loadBytes, in int version)
        {
            base.OnLoad(world, loadBytes, version);
			Initialize(world);

            int index = 0;

			if (version < 11)
				Position = SaveHelper.LoadCubePosition(loadBytes, ref index).InWorldSpace() + new Vector3(0, Cube.CUBE_SCALE, 0);
			else Position = SaveHelper.LoadVector3(loadBytes, ref index);

			if (version < 18)
			{
				var rotation = SaveHelper.LoadVector3(loadBytes, ref index);
				Rotation = Quaternion.CreateFromYawPitchRoll(rotation.X, rotation.Y, rotation.Z);
			}
			else
			{
				Rotation = SaveHelper.LoadQuat(loadBytes, ref index);
			}

			Health = SaveHelper.LoadInt32(loadBytes, ref index);
			MaxHealth = SaveHelper.LoadInt32(loadBytes, ref index);

			if (version >= 8)
            {
				Magic = SaveHelper.LoadInt32(loadBytes, ref index);
				MaxMagic = SaveHelper.LoadInt32(loadBytes, ref index);
            }

			world.InventoryManager.Get(inventory)!.Load(loadBytes, ref index);
	
			if (version >= 6)
			{
                world.InventoryManager.Get(accessoryInventory)!.Load(loadBytes, ref index);
			}

			if (version >= 9)
                world.InventoryManager.Get(gearInventory)!.Load(loadBytes, ref index);

			if (version >= 10)
				Currency = SaveHelper.LoadInt32(loadBytes, ref index);

			/*menuPlayer = new MenuPlayer(world.GameStateManager, this, inventory, craftInventory, accessoryInventory, gearInventory);
			menuPlayer.Close();*/
			//currentUI = menuPlayer;

			loadedTimeOfDay = SaveHelper.LoadFloat32(loadBytes, ref index);
            SpawnPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);

			if (version >= 14)
			{
				uint bitset = (uint)SaveHelper.LoadInt32(loadBytes, ref index);
			}

			if (version >= 11 && version < 19)
			{
				var basicState = new BasicState();
				basicState.OnLoad(loadBytes, ref index);
				if (world != null && TimeInitialized != 0)
					Set(ref basicState);
			}

			if (version >= 16)
				playerIndex = SaveHelper.LoadInt32(loadBytes, ref index);

			if (version >= 17)
			{
				playerUuid = SaveHelper.LoadInt32(loadBytes, ref index);
			}
		}

        public void Get(out BasicState state)
        {
			state = new BasicState
			{
				position = Position,
				velocity = world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear,
				rotation = Rotation,
				health = Health,
				state = (int)this.state,
				timers = { 
					[0] = this.useTimer,
					[1] = this.preUseTimer,
					[2] = this.invulnTimer,
					[3] = this.inputLockupTimer,
				},
				counters =
				{
					[0] = 0,
					// TODO these should probably go in extra data
					[2] = playerUuid,
					[3] = playerIndex,
				}
			};

			PlayerExtraState pstate = new PlayerExtraState
			{
				inventory = this.inventory,
				accessoryInventory = this.accessoryInventory,
				craftInventory = this.craftInventory,
				gearInventory = this.gearInventory,
				heldInventory = this.heldInventory,
				highlightIndex = this.highlightIndex,
				currency = this.Currency,
				useAnimTime = currentActionStats.useAnimTime,
				useAnimTimer = useAnimTimer,
				useAnimType = (int)useAnimType,

				damageTime = damageTime,
				deadTime = deadTime,
				inputLockupTimer = inputLockupTimer,

				magic = Magic,
				maxHealth = GetCalculatedMaxHealth(),
				maxMagic = GetCalculatedMaxMagic(),
			};

			state.SetExtra(ref pstate);

			pstate = state.GetExtra<PlayerExtraState>();
			Debug.Assert(pstate.useAnimType == (int)useAnimType);
        }

        public void Set(ref readonly BasicState state)
        {
			if (!IsLocalPlayer)
			{
                //var quat = state.rotation;
                //var roll = float.Atan2(2 * (quat.W * quat.X + quat.Y * quat.Z), 1 - 2 * (quat.X * quat.X + quat.Y * quat.Y));
                //var pitch = float.Asin(2 * (quat.W * quat.Y - quat.Z * quat.X));
                //var yaw = float.Atan2(2 * (quat.W * quat.Z + quat.X * quat.Y), 1 - 2 * (quat.Y * quat.Y + quat.Z * quat.Z));

                SetPositionWithOffset(state.position);
				world.PhysicsInfo.Simulation.Bodies[physicsHandle].MotionState.Velocity.Linear = state.velocity.ToNumerics();
				world.PhysicsInfo.Simulation.Awakener.AwakenBody(physicsHandle);
				//this.Rotation = new Vector3(roll, pitch, yaw);
				this.Rotation = state.rotation;
				this.Health = state.health;
				this.state = (State)state.state;
				this.useTimer = state.timers[0];
				this.preUseTimer = state.timers[1];
				this.invulnTimer = state.timers[2];
				this.inputLockupTimer = state.timers[3];
			}
        }

		public void SetPositionWithOffset(Vector3 position)
		{
            this.Position = position;
            world.PhysicsInfo.Simulation.Bodies[physicsHandle].Pose.Position = (position - BODY_OFFSET).ToNumerics();
        }

        public bool InventoryAction(int activatingPlayer, int action)
        {
			if (action == 1)
			{
				MenuPlayer.InventoryAction(world.InventoryManager.Get(craftInventory), world.InventoryManager.Get(inventory));
				return true;
			}
			return false;
        }
    }
}
