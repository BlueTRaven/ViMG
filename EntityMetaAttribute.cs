using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public class EntityMetaAttribute : Attribute
	{
		public int Version;
		//The minimum required version in order to load. Any lower than this and this entity will not load.
		//Update this when doing large, entity-saving-breaking refactors!
		public int MinVersion;

		public EntityMetaAttribute(int version, int minVersion = 0)
		{
			this.Version = version;
			this.MinVersion = minVersion;
		}
	}
}
