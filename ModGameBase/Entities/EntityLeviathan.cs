using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ViMG.Cubes;
using ViMG.Rendering;
using ViMG.VertexDeclarations;

namespace ViMG.Entities
{
    public class EntityLeviathan : Entity, IHitboxOwner
    {
        private static VerySimpleMesh quad;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("leviathan");

        public enum State
        {
			Watching,
			Enraged
        }

		public State state;
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

			// TODO: this should work for each player

			Player? furthestPlayer = null;
			float dist = float.MinValue;
			foreach (Player? player in world.player)
			{
				if (player != null)
				{
					float worldRadius = world.sizeInCubes / 2f * Cube.CUBE_SCALE;
					Vector2 worldCenter = new Vector2(worldRadius, worldRadius);
					Vector2 dirWorldCenter = new Vector2(worldCenter.X - player.Position.X, worldCenter.Y - player.Position.Z);
					float curDist = dirWorldCenter.Length();

					if (curDist > dist) 
					{
						dist = curDist;
						furthestPlayer = player;
					}
				}
            }

			const float MAX_DIST = Cube.CUBE_SCALE * 232;

			if (state == State.Watching && dist > MAX_DIST)
            {
				state = State.Enraged;

				if (hitbox == -1)
					hitbox = world.HitboxManager.Add(this, 
						new Rectangle3D(-new Vector3(Cube.CUBE_SCALE * 3), new Vector3(Cube.CUBE_SCALE * 6)).Offset(Position),
						Vector3.One, HitboxManager.Group.ENEMYHOSTILE_BOTH, 40, 0);
            }

            if (state == State.Enraged)
            {
				world.HitboxManager.Update(hitbox, new Engine.Physics.OrientedBoundingBox(Position, new(Cube.CUBE_SCALE * 3), Quaternion.Identity));
                Vector3 direction = furthestPlayer.Position - Position;
                direction.Normalize();

				Position += direction * Cube.CUBE_SCALE;
            }
        }

		//public float GetAlpha()
		//{
  //          float worldRadius = world.sizeInCubes / 2f * Cube.CUBE_SCALE;
  //          Vector2 worldCenter = new Vector2(worldRadius, worldRadius);
  //          Vector2 dirWorldCenter = new Vector2(worldCenter.X - world.player[world.localPlayerIndex].Position.X, worldCenter.Y - world.player[world.localPlayerIndex].Position.Z);
  //          float dist = dirWorldCenter.Length();

  //          const float MIN_DIST = Cube.CUBE_SCALE * 180;
  //          const float MAX_DIST = Cube.CUBE_SCALE * 224;

		//	if (dist > MIN_DIST)
		//	{
		//		return (dist - MIN_DIST) / (MAX_DIST - MIN_DIST);
		//	}
		//	else return 0;
  //      }

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
