using A1r.Input;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using Engine.Entities;
using Engine.Items;
using Engine.Networking;
using Engine.Networking.Messages;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.IMGUIImpl;
using ViMG.Items;
using ViMG.Physics;

namespace Engine.Common
{
    // Handles common player movement.
    // In general, this should be things that are directly related to input buttons being pressed.
    // Therefore it must operate on common entity state between client and server and cannot rely on server-side player state.
    public struct PlayerMovement
    {
        [ConsoleCommandVar("sv_max_vel_swimming", "maximum player velocity while swimming")]
        public static Vector3 MAX_VEL_SWIMMING = new Vector3(2.8f) * Cube.CUBE_SCALE;
        [ConsoleCommandVar("sv_max_vel_swimming_fast", "maximum player velocity while swimming fast")]
        public static Vector3 MAX_VEL_SWIMMING_FAST = new Vector3(5.6f) * Cube.CUBE_SCALE;

        [ConsoleCommandVar("sv_move_speed", "player movement speed")]
        public static float MOVE_SPEED = Cube.CUBE_SCALE * 0.8f;

        [ConsoleCommandVar("sv_max_vel", "maximum player velocity")]
        public static Vector3 MAX_VEL = Cube.CUBE_SCALE * new Vector3(3.2f, 17, 3.2f);

        public readonly bool IsLocal;
        public bool valid;
        public PlayerInput MoveLeft;
        public PlayerInput MoveRight;
        public PlayerInput MoveForward;
        public PlayerInput MoveBack;
        public PlayerInput Jump;
        public PlayerInput Run;
        public PlayerInput MoveDown;
        public PlayerInput LeftClick;
        public PlayerInput RightClick;
        public PlayerInput Throw;

        private ContactChecker contactChecker;

        public EntityManager.EntityReference playerReference;
        public int playerIndex;

        public PlayerMovement(EntityManager.EntityReference reference, int playerIndex, bool isLocal)
        {
            IsLocal = isLocal;
            valid = true;
            if (isLocal)
            {
                MoveLeft = new PlayerInput(Keys.A);
                MoveRight = new PlayerInput(Keys.D);
                MoveForward = new PlayerInput(Keys.W);
                MoveBack = new PlayerInput(Keys.S);
                Jump = new PlayerInput(Keys.Space);
                Run = new PlayerInput(Keys.LeftShift);
                MoveDown = new PlayerInput(Keys.LeftControl);
                LeftClick = new PlayerInput(MouseInput.LeftButton);
                RightClick = new PlayerInput(MouseInput.RightButton);
                Throw = new PlayerInput(Keys.Q);
            }
            else
            {
                MoveLeft = PlayerInput.NonLocalInput(Keys.A, true);
                MoveRight = PlayerInput.NonLocalInput(Keys.D, true);
                MoveForward = PlayerInput.NonLocalInput(Keys.W, true);
                MoveBack = PlayerInput.NonLocalInput(Keys.S, true);
                Jump = PlayerInput.NonLocalInput(Keys.Space, true);
                Run = PlayerInput.NonLocalInput(Keys.LeftShift, true);
                MoveDown = PlayerInput.NonLocalInput(Keys.LeftControl, true);
                LeftClick = PlayerInput.NonLocalInput(MouseInput.LeftButton, true);
                RightClick = PlayerInput.NonLocalInput(MouseInput.RightButton, true);
                Throw = PlayerInput.NonLocalInput(Keys.Q, false);
            }

            contactChecker = new ContactChecker();
        }

        public (BodyHandle, TypedIndex) MakeBody(Vector3 position, PhysicsInfo physicsInfo)
        {
            var physicsShape = new Capsule(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE * 0.98f);
            var physicsShapeIndex = physicsInfo.Simulation.Shapes.Add(physicsShape);

            var handle = physicsInfo.Simulation.Bodies.Add(BodyDescription.CreateDynamic(
                new RigidPose((position + Player.BODY_OFFSET).ToNumerics()), new BodyInertia() { InverseMass = 1f / 20f }, physicsShapeIndex, 0.001f));

            physicsInfo.Properties[handle] = new PhysicsProperties(new SubgroupCollisionFilter(FilterGroups.GROUP_PLAYER, 0), 1f);

            return (handle, physicsShapeIndex);
        }

        public void UpdateBody(PhysicsInfo physicsInfo, BodyHandle bodyHandle)
        {
            contactChecker.Update(physicsInfo, bodyHandle);
        }

        // What's our granularity here? 
        // Per-frame or per-sync?
        // per-sync is bad, drop lots of inputs at 20hz...
        public void Update(ref readonly PlayerAccumulatedStats stats, ref readonly PlayerMovement prevMovement, ref SyncedEntity player, double deltaTime, bool doSim = true)
        {
            const float MIN_NOCLIP_SPEED = Cube.CUBE_SCALE / 4f;
            const float MAX_NOCLIP_SPEED = MIN_NOCLIP_SPEED * 8;

            MoveLeft.Update();
            MoveRight.Update();
            MoveForward.Update();
            MoveBack.Update();
            Jump.Update();
            Run.Update();
            MoveDown.Update();
            LeftClick.Update();
            RightClick.Update();
            Throw.Update();

            if (!doSim) 
                return;

            var state = (Player.State)player.state;
            Vector3 fwdYO = SyncedEntity.ForwardYawOnly(ref player);
            Vector3 right = SyncedEntity.Right(ref player);

            if (state == Player.State.Noclip)
            {
                float moveSpeed = MIN_NOCLIP_SPEED;

                if (Run.Pressed())
                    moveSpeed = MAX_NOCLIP_SPEED;

                if (Main.inputManager.IsPressed(Keys.W))
                    player.position -= Vector3.Normalize(fwdYO) * moveSpeed;
                if (Main.inputManager.IsPressed(Keys.S))
                    player.position += Vector3.Normalize(fwdYO) * moveSpeed;
                if (Main.inputManager.IsPressed(Keys.A))
                    player.position -= Vector3.Normalize(right) * moveSpeed;
                if (Main.inputManager.IsPressed(Keys.D))
                    player.position += Vector3.Normalize(right) * moveSpeed;
                if (Main.inputManager.IsPressed(Keys.Space))
                    player.position += Vector3.Up * moveSpeed;
                if (Main.inputManager.IsPressed(Keys.LeftControl))
                    player.position -= Vector3.Up * moveSpeed;
            }
            else if (state == Player.State.Swimming)
            {
                UpdateMovementWater(ref player, deltaTime);
            }
            else if (state == Player.State.Normal)
            {
                UpdateMovement(in stats, in prevMovement, ref player, deltaTime);
            }
            else if (state == Player.State.Attack)
            {
                UpdateMovement(in stats, in prevMovement, ref player, deltaTime);
            }
        }

        public void UpdateMovement(ref readonly PlayerAccumulatedStats stats, ref readonly PlayerMovement prevMovement, ref SyncedEntity player, double deltaTime)
        {
            Vector3 fwdYO = SyncedEntity.ForwardYawOnly(ref player);
            Vector3 right = SyncedEntity.Right(ref player);

            var playerExtra = player.GetExtra<Player.PlayerExtraState>();

            Vector3 actualMaxVel = MAX_VEL;

            bool movementPressed = false;
            //Vector2 velXY = new Vector2(Velocity.X, Velocity.Z);
            Vector3 velocity = player.velocity;

            bool isRunning = false;
            if (Run.Pressed())
                isRunning = true;

            // TODO input lockup timer
            if (playerExtra.inputLockupTimer <= 0)
            {
                // TODO contactChecker
                //if (contactChecker.OnGround && Run.Pressed())
                //    isRunning = true;

                float actualAcceleration = MOVE_SPEED + playerExtra.accel;

                if (isRunning)
                {
                    actualMaxVel *= 1 + playerExtra.runSpeed;
                    actualAcceleration *= 2;
                }

                actualMaxVel *= new Vector3(1 + playerExtra.speed, 1, 1 + playerExtra.speed);

                Vector3 toAddToVelocity = Vector3.Zero;
                if (MoveForward.Pressed())
                {
                    toAddToVelocity -= Vector3.Normalize(fwdYO) * actualAcceleration;
                    movementPressed = true;
                }
                if (MoveBack.Pressed())
                {
                    toAddToVelocity += Vector3.Normalize(fwdYO) * actualAcceleration;
                    movementPressed = true;
                }
                if (MoveLeft.Pressed())
                {
                    toAddToVelocity -= Vector3.Normalize(right) * actualAcceleration;
                    movementPressed = true;
                }
                if (MoveRight.Pressed())
                {
                    toAddToVelocity += Vector3.Normalize(right) * actualAcceleration;
                    movementPressed = true;
                }
                // TODO contact checker
                if (contactChecker.OnGround && Jump.JustPressed(prevMovement.Jump))
                {
                    velocity.Y = Player.JUMP_SPEED + stats.JumpSpeed;
                }

                Vector2 velXZ = new Vector2(velocity.X, velocity.Z);
                float maxVelXZ = new Vector2(actualMaxVel.X, actualMaxVel.Z).Length();

                if (velXZ.Length() > maxVelXZ)
                {
                    //already above max velocity
                    //in this scenario just subtract some velocity.
                    Vector2 xz = velXZ;
                    xz -= Vector2.Normalize(xz) * actualAcceleration;
                    velocity = new Vector3(xz.X, velocity.Y, xz.Y);
                }
                Vector3 nextVelocity = velocity + toAddToVelocity;
                if (new Vector2(nextVelocity.X, nextVelocity.Z).Length() > maxVelXZ)
                {
                    //not above max velocity; set velocity to max velocity.
                    velXZ = Vector2.Normalize(new Vector2(nextVelocity.X, nextVelocity.Z)) * maxVelXZ;
                    velocity = new Vector3(velXZ.X, velocity.Y, velXZ.Y);
                }
                else
                {
                    velocity += toAddToVelocity;
                }
            }

            if (velocity.Y > Cube.CUBE_SCALE * 3.2f && !Jump.Pressed())
            {
                velocity.Y = Cube.CUBE_SCALE * 3.2f;
            }

            player.velocity = velocity;
        }

        private bool UpdateMovementWater(ref SyncedEntity player, double deltaTime)
        {
            var playerExtra = player.GetExtra<Player.PlayerExtraState>();
            Vector3 fwd = SyncedEntity.Forward(ref player);
            Vector3 right = SyncedEntity.Right(ref player);

            Vector3 actualMaxVel = MAX_VEL_SWIMMING;

            bool isRunning = false;
            bool movementPressed = false;
            if (Run.Pressed())
            {
                isRunning = true;
                actualMaxVel = MAX_VEL_SWIMMING_FAST;
            }

            float actualAcceleration = MOVE_SPEED + playerExtra.accel;

            if (isRunning)
            {
                actualMaxVel *= 1 + playerExtra.runSpeed;
                actualAcceleration *= 2;
            }

            actualMaxVel *= new Vector3(1 + playerExtra.speed, 1, 1 + playerExtra.speed);

            Vector3 toAddToVelocity = Vector3.Zero;

            if (MoveForward.Pressed())
            {
                toAddToVelocity -= Vector3.Normalize(fwd) * actualAcceleration;
                movementPressed = true;
            }
            if (MoveBack.Pressed())
            {
                toAddToVelocity += Vector3.Normalize(fwd) * actualAcceleration;
                movementPressed = true;
            }
            if (MoveLeft.Pressed())
            {
                toAddToVelocity -= Vector3.Normalize(right) * actualAcceleration;
                movementPressed = true;
            }
            if (MoveRight.Pressed())
            {
                toAddToVelocity += Vector3.Normalize(right) * actualAcceleration;
                movementPressed = true;
            }

            if (Jump.Pressed())
            {
                toAddToVelocity += Vector3.Normalize(Vector3.Up) * actualAcceleration;
                movementPressed = true;
            }
            if (MoveDown.Pressed())
            {
                toAddToVelocity -= Vector3.Normalize(Vector3.Up) * actualAcceleration;
                movementPressed = true;
            }

            Vector3 velXZ = player.velocity;
            float curMaxVel = actualMaxVel.Length();

            if (velXZ.Length() > curMaxVel)
            {
                //already above max velocity
                //in this scenario just subtract some velocity.
                Vector3 xz = velXZ;
                xz -= Vector3.Normalize(xz) * actualAcceleration;
                player.velocity = xz;
            }
            if ((player.velocity + toAddToVelocity).Length() > curMaxVel)
            {
                //not above max velocity; set velocity to max velocity.
                velXZ = Vector3.Normalize((player.velocity + toAddToVelocity)) * curMaxVel;
                player.velocity = velXZ;
            }
            else
            {
                player.velocity += toAddToVelocity;
            }

            player.velocity.Y -= PhysicsInfo.SIM_GRAVITY * 0.25f * (float)deltaTime;

            return movementPressed;
        }

        public uint GetInputBitSet(PlayerMovement prev)
        {
            SyncPlayerInputs.InputTypes pressed = SyncPlayerInputs.InputTypes.None;
            SyncPlayerInputs.InputTypes prevPressed = SyncPlayerInputs.InputTypes.None;

            if (Jump.recordedPress) pressed |= SyncPlayerInputs.InputTypes.Jump;
            if (LeftClick.recordedPress) pressed |= SyncPlayerInputs.InputTypes.LeftClick;
            if (MoveBack.recordedPress) pressed |= SyncPlayerInputs.InputTypes.MoveBack;
            if (MoveDown.recordedPress) pressed |= SyncPlayerInputs.InputTypes.MoveDown;
            if (MoveForward.recordedPress) pressed |= SyncPlayerInputs.InputTypes.MoveForward;
            if (MoveLeft.recordedPress) pressed |= SyncPlayerInputs.InputTypes.MoveLeft;
            if (MoveRight.recordedPress) pressed |= SyncPlayerInputs.InputTypes.MoveRight;
            if (RightClick.recordedPress) pressed |= SyncPlayerInputs.InputTypes.RightClick;
            if (Run.recordedPress) pressed |= SyncPlayerInputs.InputTypes.Run;
            if (Throw.recordedPress) pressed |= SyncPlayerInputs.InputTypes.Throw;

            if (prev.Jump.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.Jump;
            if (prev.LeftClick.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.LeftClick;
            if (prev.MoveBack.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.MoveBack;
            if (prev.MoveDown.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.MoveDown;
            if (prev.MoveForward.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.MoveForward;
            if (prev.MoveLeft.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.MoveLeft;
            if (prev.MoveRight.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.MoveRight;
            if (prev.RightClick.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.RightClick;
            if (prev.Run.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.Run;
            if (prev.Throw.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.Throw;

            return ((uint)pressed << sizeof(ushort)) | (uint)prevPressed;
        }

        public void SetInputBitSet(PlayerMovement prev, uint bits)
        {
            SyncPlayerInputs.InputTypes presseds = (SyncPlayerInputs.InputTypes)(ushort)(bits >> sizeof(ushort));
            SyncPlayerInputs.InputTypes prevPresseds = (SyncPlayerInputs.InputTypes)(ushort)bits;

            Jump.recordedPress = (presseds & SyncPlayerInputs.InputTypes.Jump) == SyncPlayerInputs.InputTypes.Jump;
            LeftClick.recordedPress = (presseds & SyncPlayerInputs.InputTypes.LeftClick) == SyncPlayerInputs.InputTypes.LeftClick;
            MoveBack.recordedPress = (presseds & SyncPlayerInputs.InputTypes.MoveBack) == SyncPlayerInputs.InputTypes.MoveBack;
            MoveDown.recordedPress = (presseds & SyncPlayerInputs.InputTypes.MoveDown) == SyncPlayerInputs.InputTypes.MoveDown;
            MoveForward.recordedPress = (presseds & SyncPlayerInputs.InputTypes.MoveForward) == SyncPlayerInputs.InputTypes.MoveForward;
            MoveLeft.recordedPress = (presseds & SyncPlayerInputs.InputTypes.MoveLeft) == SyncPlayerInputs.InputTypes.MoveLeft;
            MoveRight.recordedPress = (presseds & SyncPlayerInputs.InputTypes.MoveRight) == SyncPlayerInputs.InputTypes.MoveRight;
            RightClick.recordedPress = (presseds & SyncPlayerInputs.InputTypes.RightClick) == SyncPlayerInputs.InputTypes.RightClick;
            Run.recordedPress = (presseds & SyncPlayerInputs.InputTypes.Run) == SyncPlayerInputs.InputTypes.Run;
            Throw.recordedPress = (presseds & SyncPlayerInputs.InputTypes.Throw) == SyncPlayerInputs.InputTypes.Throw;

            prev.Jump.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.Jump) == SyncPlayerInputs.InputTypes.Jump;
            prev.LeftClick.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.LeftClick) == SyncPlayerInputs.InputTypes.LeftClick;
            prev.MoveBack.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.MoveBack) == SyncPlayerInputs.InputTypes.MoveBack;
            prev.MoveDown.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.MoveDown) == SyncPlayerInputs.InputTypes.MoveDown;
            prev.MoveForward.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.MoveForward) == SyncPlayerInputs.InputTypes.MoveForward;
            prev.MoveLeft.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.MoveLeft) == SyncPlayerInputs.InputTypes.MoveLeft;
            prev.MoveRight.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.MoveRight) == SyncPlayerInputs.InputTypes.MoveRight;
            prev.RightClick.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.RightClick) == SyncPlayerInputs.InputTypes.RightClick;
            prev.Run.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.Run) == SyncPlayerInputs.InputTypes.Run;
            prev.Throw.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.Throw) == SyncPlayerInputs.InputTypes.Throw;
        }
    }
}
