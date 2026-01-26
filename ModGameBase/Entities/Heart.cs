using BrUtility;
using Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Entities
{
	[EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
	[EntityMeta(0)]
    public class Heart : Entity, IHitboxOwner
    {
        private static VerySimpleMesh mesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("heart");

        public int Health;
		public int MaxHealth = 20;

		private float invulnTimer;

		private int hitbox = -1;
		private Rectangle3D bounds = new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 2f), new Vector3(Cube.CUBE_SCALE));

		private float alive;

        public Heart()
        {
            DoesSync = false;
            DoesMajorSync = false;
        }

        public Heart(Vector3 position) 
        {
            DoesSync = false;
            DoesMajorSync = false;
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
			else world.HitboxManager.Update(hitbox, bounds.Offset(Position).ToOBB());

			for (int i = 0; i < World.MAX_PLAYERS; i++)
			{
				Player player = world.player[i];
				if (player != null)
				{
					if ((player.Position - Position).Length() < Cube.CUBE_SCALE * 32)
					{
						// TODO there are better ways to do this behavior. Entities should generally not have to be aware of networking
						if (GlobalState.GameStateManager.netMode != GameStates.GameStateManager.NetworkingMode.Client)
						{
							world.PassiveSpawnerManager.SpawnCapMultiplier = 2f;
							world.PassiveSpawnerManager.SpawnChanceMultipler = 2f;
							player.GetBuffManager().AddBuff(new Buffs.Buff.BuffInstance(GlobalState.Registry.BuffRegistry.Get("heart_enemy_spawnrate_increase"), 1));
						}
					}
				}
			}
        }

        public override void OnUnload()
        {
            base.OnUnload();

			if (hitbox != -1)
				world.HitboxManager.Remove(hitbox);

			// TODO there are better ways to do this behavior. Entities should generally not have to be aware of networking
			if (GlobalState.GameStateManager.netMode != GameStates.GameStateManager.NetworkingMode.Client)
			{
				world.PassiveSpawnerManager.SpawnCapMultiplier = 1f;
				world.PassiveSpawnerManager.SpawnChanceMultipler = 1f;
			}
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
						world.EntityManager.Kill(this);
					}

					invulnTimer = 0.25f;
				}
			}
		}

		//public override void Draw(GraphicsDevice device, Effect effect)
		//{
		//	base.Draw(device, effect);

		//	if (mesh.IBO == null)
  //              mesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE, Enums.Alignment.Bottom);
  //          //mesh = MeshHelper.MakeEnemyQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE);

  //          float healthPercent = (float)Health / (float)MaxHealth;

		//	float interval = MathHelper.Lerp(0.25f, 2f, healthPercent);

		//	float t = (alive % interval) / interval;

		//	float s = MathF.Sin(MathF.PI * 2 * t) * 0.5f + 0.5f;

		//	float scale = MathHelper.Lerp(0.75f, 1.15f, s);

		//	Vector3 tintColor = invulnTimer > 0 ? Color.Red.ToVector3() : Color.White.ToVector3();

		//	Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
		//		Matrix.CreateTranslation(-new Vector3(0, Cube.CUBE_SCALE / 2f, 0)) *
		//		Matrix.CreateScale(scale) *
		//		Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
		//		Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
		//		Matrix.CreateTranslation(Position), new RectangleF(0, 0, 16, 21), tintColor));

		//	if (Health < MaxHealth)
		//		DrawHelper3D.DrawHealthbar(device, Health, MaxHealth, Position);
		//}

        public override void OnSave(List<byte> saveBytes)
        {
            base.OnSave(saveBytes);

			SaveHelper.SaveVector3(saveBytes, Position);

			SaveHelper.SaveInt32(saveBytes, Health);
        }

        public override void OnLoad(World world, byte[] loadBytes, in int version)
        {
            base.OnLoad(world, loadBytes, version);

            int index = 0;
			Position = SaveHelper.LoadVector3(loadBytes, ref index);

			Health = SaveHelper.LoadInt32(loadBytes, ref index);
        }
    }
}
