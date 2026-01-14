using A1r.Input;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Entities
{
    public struct PlayerInput
    {
        public enum Type
        {
            Key,
            Controller,
            Mouse,
        }

        public Type type;
        public Input input;
        public Keys key;
        public MouseInput mouse;

        public bool isLocalInput = true;
        public bool continues; // If client input, if an artificialPress is received, artificialPress will not be reset
        //public bool previousRecordedPress;
        public bool recordedPress;
        private bool forcedUnpress;

        public PlayerInput(Input input)
        {
            this.isLocalInput = true;
            this.input = input;
            this.type = Type.Controller;
        }

        public PlayerInput(Keys key)
        {
            this.isLocalInput = true;
            this.key = key;
            this.type = Type.Key;
        }

        public PlayerInput(MouseInput mouse)
        {
            this.isLocalInput = true;
            this.mouse = mouse;
            this.type = Type.Mouse;
        }

        public static PlayerInput NonLocalInput(Input input, bool continues)
        {
            var i = new PlayerInput(input);
            i.isLocalInput = false;
            i.continues = continues;

            return i;
        }

        public static PlayerInput NonLocalInput(Keys key, bool continues)
        {
            var i = new PlayerInput(key);
            i.isLocalInput = false;
            i.continues = continues;

            return i;
        }

        public static PlayerInput NonLocalInput(MouseInput mouse, bool continues)
        {
            var i = new PlayerInput(mouse);
            i.isLocalInput = false;
            i.continues = continues;

            return i;
        }

        public void Update()
        {
            if (isLocalInput)
            {
                recordedPress = type switch
                {
                    Type.Mouse => Main.inputManager.IsPressed(mouse),
                    Type.Key => Main.inputManager.IsPressed(key),
                    Type.Controller => Main.inputManager.IsPressed(input),
                    _ => throw new NotImplementedException(),
                };
            }
            else
            {
                if (!continues) recordedPress = false;
            }
        }

        public bool Changed(PlayerInput prev)
        {
            if (forcedUnpress) return true;

            return recordedPress != prev.recordedPress;
        }

        public bool JustPressed(PlayerInput prev)
        {
            return !prev.recordedPress && recordedPress;
        }

        public bool Pressed()
        {
            return recordedPress;
        }

        public void ForceUnpress()
        {
            recordedPress = false;
            forcedUnpress = true;
        }
    }
}
