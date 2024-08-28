using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;
using ViMG.VertexDeclarations;

namespace ViMG.Entities
{
    public class EntityLeviathan : Entity, IHitboxOwner
    {
        private static VerySimpleMesh quad;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("leviathan");

        private enum State
        {
			Watching,
			Enraged
        }

		private State state;
		private int hitbox = -1;

        public EntityLeviathan()
        {

        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

			base.AlwaysRender = true;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

			float worldRadius = world.sizeInCubes / 2f * Cube.CUBE_SCALE;
			Vector2 worldCenter = new Vector2(worldRadius, worldRadius);
			Vector2 dirWorldCenter = new Vector2(worldCenter.X - world.player.Position.X, worldCenter.Y - world.player.Position.Z);
			float dist = dirWorldCenter.Length();

			const float MAX_DIST = Cube.CUBE_SCALE * 232;

			if (state == State.Watching && dist > MAX_DIST)
            {
				state = State.Enraged;

				Position = world.player.Position - Main.camera.ForwardYawOnly * Cube.CUBE_SCALE * 32;

				if (hitbox == -1)
					hitbox = world.HitboxManager.Add(this, 
						new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 3), new Vector3(Cube.CUBE_SCALE * 6)).Offset(Position),
						Vector3.One, HitboxManager.Group.ENEMYHOSTILE_BOTH, 40, 0);
            }

            if (state == State.Enraged)
            {
				world.HitboxManager.Update(hitbox, new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 3), new Vector3(Cube.CUBE_SCALE * 6)).Offset(Position));
                Vector3 direction = world.player.Position - Position;
                direction.Normalize();

				Position += direction * Cube.CUBE_SCALE;
            }
        }

        public override void Draw(GraphicsDevice device, Effect effect)
        {
            base.Draw(device, effect);

			if (quad.IBO == null)
			{
                FastList<VertexCube> vertices = new FastList<VertexCube>();
                List<int> indices = new List<int>();

				indices.Add(0);
				indices.Add(1);
				indices.Add(3);
				indices.Add(1);
				indices.Add(2);
				indices.Add(3);

				const float VERT_DIST = Cube.CUBE_SCALE * 6;
				vertices.Add(new VertexCube(new Vector3(-VERT_DIST, -VERT_DIST, 0), Color.White, new Vector2(0, 1), new Vector3(0, 0, -1)));
				vertices.Add(new VertexCube(new Vector3(-VERT_DIST, VERT_DIST, 0), Color.White, new Vector2(0, 0), new Vector3(0, 0, -1)));
				vertices.Add(new VertexCube(new Vector3(VERT_DIST, VERT_DIST, 0), Color.White, new Vector2(1, 0), new Vector3(0, 0, -1)));
				vertices.Add(new VertexCube(new Vector3(VERT_DIST, -VERT_DIST, 0), Color.White, new Vector2(1, 1), new Vector3(0, 0, -1)));

				//quad = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices);
				quad = VerySimpleMesh.Opaque(device, new ChunkRenderMesher.VertexAttributes(vertices, indices));
			}
			else
			{
				if (state == State.Watching)
				{
					float worldRadius = world.sizeInCubes / 2f * Cube.CUBE_SCALE;
					Vector2 worldCenter = new Vector2(worldRadius, worldRadius);
					Vector2 dirWorldCenter = new Vector2(worldCenter.X - world.player.Position.X, worldCenter.Y - world.player.Position.Z);
					float dist = dirWorldCenter.Length();

					const float MIN_DIST = Cube.CUBE_SCALE * 180;
					const float MAX_DIST = Cube.CUBE_SCALE * 224;

					//TODO: if dist > 232, do the thing...

					if (dist > Cube.CUBE_SCALE * 180)
					{
						Vector3 tpos = world.player.Position - Main.camera.ForwardYawOnly * Cube.CUBE_SCALE * 32;

						float alpha = (dist - MIN_DIST) / (MAX_DIST - MIN_DIST);

						Main.Renderer.AddTransparentDraw(new Rendering.RendererDeferred.TransparentDraw(Cube.CUBE_SCALE * 32,
							material, quad, Matrix.CreateRotationY(-Main.camera.Rotation.Y) * Matrix.CreateTranslation(tpos), 
							new RectangleF(0, 0, 64, 64), Color.White * alpha));
					}
				}
                else
                {
					Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(
						material, quad, Matrix.CreateRotationY(-Main.camera.Rotation.Y) * Matrix.CreateTranslation(Position),
						new RectangleF(64, 0, 64, 64)));
				}
			}
		}

        public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
        {
			if (other.group == HitboxManager.Group.PLAYER_TAKE)
            {
				if (other.owner is Player player)
                {
					//instantly kill the player and return to normal
					player.Kill();

					Position = Vector3.Zero;
					world.HitboxManager.Remove(hitbox);
					hitbox = -1;
					state = State.Watching;
                }
            }
        }
    }
}
