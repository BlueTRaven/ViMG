using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;

namespace ViMG.Entities
{
    public class Ghoul : Entity, IHasStats
    {
        public AIWalkerMelee<Ghoul> ai;
		private NoticeHandler<Player> noticeHandler;
		private BuffManager buffManager;

		private float alive;
        private (VertexBuffer VBO, IndexBuffer IBO) mesh;

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

			ai.OnUnload();
		}

		public override void Initialize(World world)
		{
			base.Initialize(world);

			buffManager = new BuffManager(this);
			noticeHandler = new NoticeHandler<Player>(this, Cube.CUBE_SCALE * 16, false);

			ai = new AIWalkerMelee<Ghoul>(this, new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 0.35f, 0, Cube.CUBE_SCALE * 0.35f),
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

				if (Main.camera.GetFrustum().Intersects(sphere))
				{
					for (int i = 0; i < LightManager.MAX_LIGHTS; i++)
					{
						LightManager.Light light = world.LightManager.Get(i);

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
                else
                {
					inLight = false;
                }
            }

			prevInLight = inLight;
			if (inLight)
				ai.SetPaused();

			ai.Update(deltaTime);

			if ((world.player.Position - Position).Length() > 128 * Cube.CUBE_SCALE)
				world.EntityManager.Remove(this);
		}

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

			if (mesh.VBO == null)
				mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 2);

			if (!inLight)
			{
				Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("ghoul"),
					DrawHelper.BlackPixel, Main.assetsManager.GetAsset<Texture2D>("ghoul_emissive"), mesh.VBO, mesh.IBO,
					Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
					Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
					Matrix.CreateTranslation(Position), new RectangleF(0, 0, 16, 32)));
			}
            else
            {
				float distance = (Main.camera.Position - Position).Length();

				float alpha = MathHelper.Lerp(0.5f, 1f, inLightTimer / CHECK_LIGHT_TIME);

				Main.Renderer.DrawsTransparentPass.Add(new Rendering.RendererDeferred.TransparentDraw(distance,
					Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
					Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
					Matrix.CreateTranslation(Position),
					Main.assetsManager.GetAsset<Texture2D>("ghoul"),
					Main.assetsManager.GetAsset<Texture2D>("ghoul_emissive"), mesh.VBO, mesh.IBO, new RectangleF(0, 0, 16, 32), Color.White * alpha));
            }

			if (ai.Health < ai.MaxHealth)
				DrawHelper3D.DrawHealthbar(device, ai.Health, ai.MaxHealth, Position + new Vector3(0, Cube.CUBE_SCALE / 2f, 0));
		}

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
