using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class BuffWellFed : Buff
    {
        public BuffWellFed() : base("well_fed", 999f, 8f, Main.assetsManager.GetAsset<Texture2D>("skill"), new RectangleF(192, 224, 16, 16))
        {
            Name = "Well Fed";
            Description = "Recently ate.\n" +
                "Maximum health is increased.\n" +
                "Regenerating Health Slowly.";
        }

        public override void Update(double deltaTime, ref BuffInstance buffInstance, ref Player.AccumulatedStats stats)
        {
            base.Update(deltaTime, ref buffInstance, ref stats);

            stats.HPFlat += 10;
            stats.HPRegenAmt += 1;
            stats.HPRegenTime += 8f;
        }
    }
}
