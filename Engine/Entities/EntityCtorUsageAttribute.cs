using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Entities
{
    [Flags]
    public enum EntityCtorUsageType
    {
        // Usable for serialization construction
        Serialization = 1 << 0,
        // Usable for new construction. If not set, then the EntityType for this entity may not use New to construct it.
        New = 1 << 1,
        All = Serialization | New,
    }

    [AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = true)]
    public sealed class EntityCtorUsageAttribute : Attribute
    {
        public readonly EntityCtorUsageType usage;

        public EntityCtorUsageAttribute(EntityCtorUsageType usage)
        {
            this.usage = usage;
        }
    }
}
