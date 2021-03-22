using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Entities
{
	public class NoticeHandler<TDetect> where TDetect : Entity
	{
		public bool Noticed;

		private TDetect noticedEntity;

		private Entity entity;
		private float detectRadius;
		private bool requiresLineOfSight;

		public NoticeHandler(Entity entity, float detectRadius, bool requiresLineOfSight)
		{
			this.entity = entity;
			this.detectRadius = detectRadius;
			this.requiresLineOfSight = requiresLineOfSight;
		}

		public void Update()
		{
			var detectables = entity.world.EntityManager.GetAll<TDetect>();

			foreach (Entity ent in detectables)
			{
				if ((ent.Position - entity.Position).Length() < detectRadius)
				{
					noticedEntity = ent as TDetect;
					Noticed = true;
				}
			}
		}

		public TDetect GetNoticedEntity()
		{
			return noticedEntity;
		}

		public void OnTakeDamage()
		{
			Noticed = true;
		}
	}
}
