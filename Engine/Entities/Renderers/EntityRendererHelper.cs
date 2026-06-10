using Engine.Networking;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Entities.Renderers
{
    public static class EntityRendererHelper
    {
        public static Color GetHurtColor(in SyncedEntity entity, int timer)
        {
            float p = 0;
            if (entity.aliveTime > 0.5f)
            {
                float invulnTime = entity.timers[3];
                float MIN = 0.15f;
                float MAX = 0.25f;
                p = 1 - float.Clamp((invulnTime - MIN) / (MIN - MAX), 0f, 1f);
            }
            return Color.Lerp(Color.White, Color.Red, p);
        }
    }
}
