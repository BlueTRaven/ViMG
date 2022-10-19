using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Entities
{
	public class NoticeHandler<TDetect> where TDetect : Entity
	{
		public bool Noticed;

		public TDetect Target;

		private Entity entity;
		private float detectRadius;
		private bool requiresLineOfSight;

		private readonly float noticeFalloffTime;
		private float noticeFalloffTimer;

		public NoticeHandler(Entity entity, float detectRadius, bool requiresLineOfSight, float noticeFalloffTime = 20)
		{
			this.entity = entity;
			this.detectRadius = detectRadius;
			this.requiresLineOfSight = requiresLineOfSight;

			this.noticeFalloffTime = noticeFalloffTime;
			noticeFalloffTimer = noticeFalloffTime;
		}

		public void Update(double deltaTime)
		{
			var detectables = entity.world.EntityManager.GetAll<TDetect>();

			foreach (Entity ent in detectables)
			{
				if ((ent.Position - entity.Position).Length() < detectRadius)
				{
					Target = ent as TDetect;
					Noticed = true;

					noticeFalloffTimer = noticeFalloffTime;
				}
			}

			if (Noticed)
			{
				noticeFalloffTimer -= (float)deltaTime;

				if (noticeFalloffTimer <= 0 || Target == null || Target.Dead)
				{
					Noticed = false;
					Target = null;

					noticeFalloffTimer = noticeFalloffTime;
				}
			}
		}

		public TDetect GetNoticedEntity()
		{
			return Target;
		}

		public void OnTakeDamage(IHitboxOwner entity)
		{
			if (entity.GetType() == typeof(TDetect))
			{
				Noticed = true;
				Target = entity as TDetect;
				noticeFalloffTimer = noticeFalloffTime;
			}
		}
	}
}
