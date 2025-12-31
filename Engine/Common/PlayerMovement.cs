using A1r.Input;
using Engine.Entities;
using Engine.Networking;
using Engine.Networking.Messages;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;

namespace Engine.Common
{
    // Handles common player movement.
    // In general, this should be things that are directly related to input buttons being pressed.
    // Therefore it must operate on common entity state between client and server and cannot rely on server-side player state.
    public struct PlayerMovement
    {
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

        public EntityManager.EntityReference playerReference;
        public int playerIndex;

        public PlayerMovement(EntityManager.EntityReference reference, int playerIndex, bool isLocal)
        {
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
            }
        }

        // What's our granularity here? 
        // Per-frame or per-sync?
        // per-sync is bad, drop lots of inputs at 20hz...
        public void Update(ref BasicState player)
        {
            MoveLeft.Update();
            MoveRight.Update();
            MoveForward.Update();
            MoveBack.Update();
            Jump.Update();
            Run.Update();
            MoveDown.Update();
            LeftClick.Update();
            RightClick.Update();
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

            if (prev.Jump.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.Jump;
            if (prev.LeftClick.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.LeftClick;
            if (prev.MoveBack.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.MoveBack;
            if (prev.MoveDown.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.MoveDown;
            if (prev.MoveForward.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.MoveForward;
            if (prev.MoveLeft.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.MoveLeft;
            if (prev.MoveRight.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.MoveRight;
            if (prev.RightClick.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.RightClick;
            if (prev.Run.recordedPress) prevPressed |= SyncPlayerInputs.InputTypes.Run;

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

            prev.Jump.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.Jump) == SyncPlayerInputs.InputTypes.Jump;
            prev.LeftClick.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.LeftClick) == SyncPlayerInputs.InputTypes.LeftClick;
            prev.MoveBack.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.MoveBack) == SyncPlayerInputs.InputTypes.MoveBack;
            prev.MoveDown.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.MoveDown) == SyncPlayerInputs.InputTypes.MoveDown;
            prev.MoveForward.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.MoveForward) == SyncPlayerInputs.InputTypes.MoveForward;
            prev.MoveLeft.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.MoveLeft) == SyncPlayerInputs.InputTypes.MoveLeft;
            prev.MoveRight.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.MoveRight) == SyncPlayerInputs.InputTypes.MoveRight;
            prev.RightClick.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.RightClick) == SyncPlayerInputs.InputTypes.RightClick;
            prev.Run.recordedPress = (prevPresseds & SyncPlayerInputs.InputTypes.Run) == SyncPlayerInputs.InputTypes.Run;
        }
    }
}
