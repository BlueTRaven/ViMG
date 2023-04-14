using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Entities.Renderers
{
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class EntityRenderedByAttribute : Attribute
    {
        public readonly Type type;

        public EntityRenderedByAttribute(Type type)
        {
            this.type = type;
        }
    }
}
