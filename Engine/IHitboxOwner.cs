using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public interface IHitboxOwner
	{
		public void OnInteractWithOther(HitboxManager.Hitbox us, HitboxManager.Hitbox other);
	}
}
