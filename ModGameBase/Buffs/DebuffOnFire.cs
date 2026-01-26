using BrUtility;
using Engine;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class DebuffOnFire : Buff
    {
        private static Buff fireResist;
        public DebuffOnFire() : base("on_fire", 999f, 1f, new RectangleF(176, 224, 16, 16))
        {
            Name = "On Fire";
            Description = "It feels like maybe you're on fire.";
        }

        public override void LoadContent(GraphicsDevice device)
        {
            base.LoadContent(device);
            this.texture = GlobalState.AssetsManager.GetAsset<Texture2D>("skill");
        }

        public override void Tick(double deltaTime, IBuffManager manager, ref BuffInstance buffInstance, ref Stats stats)
        {
            base.Tick(deltaTime, manager, ref buffInstance, ref stats);

            if (fireResist == null)
                fireResist = GlobalState.Registry.BuffRegistry.Get("fire_resist");

            if (manager.HasBuff(fireResist))
                stats.HP -= int.Max(1, (int)(buffInstance.stack * 0.8f));
            else stats.HP -= 1;
        }

        public override void Tick(double deltaTime, IBuffManager manager, Player player, ref BuffInstance buffInstance, ref Player.AccumulatedStats stats)
        {
            base.Tick(deltaTime, manager, player, ref buffInstance, ref stats);
            
            if (fireResist == null)
                fireResist = GlobalState.Registry.BuffRegistry.Get("fire_resist");

            if (manager.HasBuff(fireResist))
                player.Health -= int.Max(1, (int)(buffInstance.stack * 0.8f));
            else player.Health -= buffInstance.stack;
        }
    }
}
