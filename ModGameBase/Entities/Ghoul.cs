using BrUtility;
using Engine.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Entities
{
	// TODO: this should check for sunlight too somehow!
    public class Ghoul : Entity, IHasStats
    {
        private static VerySimpleMesh mesh;
		private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("ghoul");

        public AIWalkerMelee ai;
		private NoticeHandler<Player> noticeHandler;
		private BuffManager buffManager;

		private float alive;

		private const float CHECK_LIGHT_TIME = 1f;
		private float checkLightTimer;
		private float inLightTimer;
		private const float MAX_LIGHT_SCALE = 0.2f;
		private bool inLight;
		private bool prevInLight;

		private int maxHealth = 24;

		private BoundingSphere sphere;

        public Ghoul()
		{

		}

		public Ghoul(Vector3 position)
		{
			this.Position = position;
		}

		public override void OnUnload()
		{
			base.OnUnload();

            var funcs = new AIWalkerMelee.Funcs<Ghoul> { ai = ai, entity = this };
            funcs.OnUnload();
		}

		public override void Initialize(World world)
		{
			base.Initialize(world);

			buffManager = new BuffManager(this);
			noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);

			ai = new AIWalkerMelee(new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
				new Vector3(Cube.CUBE_SCALE * 0.7f, Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 0.7f)),
				new Rectangle3D(-new Vector3(Cube.CUBE_SCALE), new Vector3(Cube.CUBE_SCALE * 2f)),
				noticeHandler,
				buffManager,
				maxHealth);
		}

		public override void Update(double deltaTime)
		{
			sphere = new BoundingSphere(Position, Cube.CUBE_SCALE * 2f);

			alive += (float)deltaTime;

			checkLightTimer -= (float)deltaTime;

			if (inLightTimer > 0)
				inLightTimer -= (float)deltaTime;

			if (checkLightTimer <= 0)
            {
				inLight = false;
				float accumLightScale = 0;

				checkLightTimer = CHECK_LIGHT_TIME;

				for (int i = 0; i < LightManager.LightsMax; i++)
				{
					// TODO: we want to check all lights here to see if the ghoul is inside one.
					// Unfortunately this doesn't work too terribly well with LightManager2, since it's immediate mode, and all Lights that are
					// present in the world may not be added yet.
					// Some sort of double-buffering will probably be necessary.
					var light = world.LightManager2.Get(i);
					//LightManager.Light light = world.LightManager.Get(i);

					if (light.active)
					{
						Vector3 distance = light.position - Position;

						if (distance.Length() < light.end)
						{
							float lightScale = 1 - Math.Clamp((distance.Length() - light.start) / (light.end - light.start), 0f, 1f);
							accumLightScale += lightScale;

							if (accumLightScale >= MAX_LIGHT_SCALE)
							{
								if (!prevInLight)
									inLightTimer = CHECK_LIGHT_TIME;
								inLight = true;
								break;
							}
						}
					}
				}
            }

			prevInLight = inLight;
            var funcs = new AIWalkerMelee.Funcs<Ghoul> { ai = ai, entity = this };
            if (inLight)
				funcs.SetPaused();

			funcs.Update(deltaTime);

            if (world.player.All(x => x == null || (x.Position - Position).Length() > 128 * Cube.CUBE_SCALE))
                world.EntityManager.Kill(this);
		}

		public bool IsInLight() => this.inLight;
		public float GetAlpha() => inLightTimer / CHECK_LIGHT_TIME;

  //      public override void Draw(GraphicsDevice device, Effect effect)
  //      {
  //          base.Draw(device, effect);

		//	if (mesh.IBO== null)
		//		mesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 2, Enums.Alignment.Bottom);
  //          //mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 2);

  //          if (!inLight)
		//	{
		//		Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
		//			Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
		//			Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
		//			Matrix.CreateTranslation(Position), new RectangleF(0, 0, 16, 32)));
		//	}
  //          else
  //          {
		//		float distance = (Main.camera.Position - Position).Length();

		//		float alpha = MathHelper.Lerp(0.5f, 1f, inLightTimer / CHECK_LIGHT_TIME);

		//		Main.Renderer.AddTransparentDraw(new Rendering.RendererDeferred.TransparentDraw(distance,
		//			material, mesh,
  //                  Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
  //                  Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
  //                  Matrix.CreateTranslation(Position), 
		//			new RectangleF(0, 0, 16, 32), Color.White * alpha));
  //          }

		//	if (ai.Health < ai.MaxHealth)
		//		DrawHelper3D.DrawHealthbar(device, ai.Health, ai.MaxHealth, Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0));
		//}

        public Stats GetStats()
        {
            throw new NotImplementedException();
        }

        public void SetStats(Stats stats)
        {
            throw new NotImplementedException();
        }
    }
}
