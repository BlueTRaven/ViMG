using BrUtility;
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
	[EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
	[EntityMeta(0)]
    public class Heart : Entity, IHitboxOwner
    {
        private (VertexBuffer VBO, IndexBuffer IBO) mesh;

		public int Health;
		public int MaxHealth = 20;

		private float invulnTimer;

		private int hitbox = -1;
		private Rectangle3D bounds = new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 2f), new Vector3(Cube.CUBE_SCALE));

		private float alive;

        public Heart()
        {

        }

        public Heart(Vector3 position) 
        {
            this.Position = position;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

			Health = MaxHealth;
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

			alive += (float)deltaTime;

			invulnTimer -= (float)deltaTime;

			if (hitbox == -1)
				hitbox = world.HitboxManager.Add(this, bounds.Offset(Position), Vector3.Zero, HitboxManager.Group.ENEMYHOSTILE_TAKE, 1, 1);
			else world.HitboxManager.Update(hitbox, bounds.Offset(Position));

			if ((world.player.Position - Position).Length() < Cube.CUBE_SCALE * 32)
            {
				world.PassiveSpawnerManager.SpawnCapMultiplier = 2f;
				world.PassiveSpawnerManager.SpawnChanceMultipler = 2f;
				world.player.GetBuffManager().AddBuff(new Buffs.Buff.BuffInstance(Main.Registry.BuffRegistry.Get("heart_enemy_spawnrate_increase"), 1));
            }
        }

        public override void OnUnload()
        {
            base.OnUnload();

			if (hitbox != -1)
				world.HitboxManager.Remove(hitbox);

			world.PassiveSpawnerManager.SpawnCapMultiplier = 1f;
			world.PassiveSpawnerManager.SpawnChanceMultipler = 1f;
        }

        public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other)
		{
			if (invulnTimer <= 0)
			{
				if (other.group == HitboxManager.Group.PLAYER_DEAL)
				{
					Health -= other.damage;

					if (Health <= 0)
					{
						Health = 0;
						world.EntityManager.Remove(this);
					}

					invulnTimer = 0.25f;
				}
			}
		}

		public override void Draw(GraphicsDevice device, Effect effect)
		{
			base.Draw(device, effect);

			if (mesh.VBO == null)
				mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);

			float healthPercent = (float)Health / (float)MaxHealth;

			float interval = MathHelper.Lerp(0.25f, 2f, healthPercent);

			float t = (alive % interval) / interval;

			float s = MathF.Sin(MathF.PI * 2 * t) * 0.5f + 0.5f;

			float scale = MathHelper.Lerp(0.75f, 1.15f, s);

			Vector3 tintColor = invulnTimer > 0 ? Color.Red.ToVector3() : Color.White.ToVector3();

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("heart"),
				DrawHelper.BlackPixel, DrawHelper.BlackPixel, mesh.VBO, mesh.IBO,
				Matrix.CreateTranslation(-new Vector3(0, Cube.CUBE_SCALE / 2f, 0)) *
				Matrix.CreateScale(scale) *
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(Position), new RectangleF(0, 0, -16, 21), tintColor));

			if (Health < MaxHealth)
				DrawHelper3D.DrawHealthbar(device, Health, MaxHealth, Position);
		}

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

			SaveHelper.SaveVector3(saveBytes, Position);

			SaveHelper.SaveInt32(saveBytes, Health);
        }

        public override void OnLoad(byte[] loadBytes, in int version)
        {
            base.OnLoad(loadBytes, version);

			int index = 0;
			Position = SaveHelper.LoadVector3(loadBytes, ref index);

			Health = SaveHelper.LoadInt32(loadBytes, ref index);
        }
    }
}
