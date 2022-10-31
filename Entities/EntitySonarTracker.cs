using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Entities
{
    public class EntitySonarTracker : Entity
    {
        private const float EXPAND_TIME = 0.5f;
        private const float TOTAL_TIME = 8f;

        private static EntitySonarTracker instance;

        private float alive;

        public EntitySonarTracker(Vector3 position)
        {
            this.Position = position;

            CanBecomeInactive = false;
            AlwaysRender = true;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            if (instance != null && instance != this)
            {
                world.EntityManager.Remove(instance);
                instance = this;
            }
        }

        public override void OnUnload()
        {
            base.OnUnload();

            Main.Renderer.EffectEmptyEnabled = false;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            Main.Renderer.EffectEmptyEnabled = true;

            alive += (float)deltaTime;

            float sonarAlpha;
            float sonarr;
            if (alive < EXPAND_TIME)
            {
                sonarr = Cube.CUBE_SCALE * 32f * (alive / EXPAND_TIME);
                sonarAlpha = 1f;
            }
            else
            {
                sonarr = Cube.CUBE_SCALE * 32f;
                sonarAlpha = 1 - ((alive - EXPAND_TIME) / (TOTAL_TIME - EXPAND_TIME));
            }

            if (alive >= TOTAL_TIME)
            {
                world.EntityManager.Remove(this);
            }

            Main.Renderer.EffectEmpty.Parameters["TintColor"].SetValue((Color.CornflowerBlue * 0.75f * sonarAlpha).ToVector4());
            Main.Renderer.EffectEmpty.Parameters["PositionRadius"].SetValue(new Vector4(Main.camera.Position, sonarr));
        }

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);


        }
    }
}
