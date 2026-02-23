using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Items
{
    public interface IJumpEffect
    {
        void DoJumpCommon(float jumpSpeed, ref Vector3 velocity);
        void DoJumpServer(Player player, float jumpSpeed, ref Vector3 velocity);
    }

    public class DefaultJumpEffect : IJumpEffect
    {
        private static DefaultJumpEffect instance;
        public static DefaultJumpEffect Instance
        {
            get
            {
                if (instance == null)
                    instance = new DefaultJumpEffect();

                return instance;
            }
        }

        public void DoJumpCommon(float jumpSpeed, ref Vector3 velocity)
        {
            velocity.Y = jumpSpeed;
        }

        public void DoJumpServer(Player player, float jumpSpeed, ref Vector3 velocity)
        {
        }
    }

    public class SuperjumpJumpEffect : IJumpEffect
    {
        private static SuperjumpJumpEffect instance;
        public static SuperjumpJumpEffect Instance
        {
            get
            {
                if (instance == null)
                    instance = new SuperjumpJumpEffect();

                return instance;
            }
        }

        public void DoJumpCommon(Player player, float jumpSpeed, ref Vector3 velocity)
        {
            velocity.Y = jumpSpeed * 8;
        }

        public void DoJumpCommon(float jumpSpeed, ref Vector3 velocity)
        {
            velocity.Y = jumpSpeed * 8;
        }

        public void DoJumpServer(Player player, float jumpSpeed, ref Vector3 velocity)
        {
        }
    }
}
