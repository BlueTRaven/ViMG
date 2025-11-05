using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Client
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RegisterClientEntityAttribute : Attribute
    {
        public Type entityType;
        public RegisterClientEntityAttribute(Type entityType)
        {
            this.entityType = entityType;
        }
    }
}
