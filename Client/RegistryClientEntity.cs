using BrUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;
using ViMG.Entities.Renderers;

namespace ViMG.Client
{
    public sealed class RegisteredClientEntity : IRegisterable
    {
        public readonly Type entityType;

        public string Identifier { get; init; }

        public RegisteredClientEntity(string identifier, Type entityType)
        {
            Identifier = identifier;
            this.entityType = entityType;
        }
    }

    public class RegistryClientEntity : ObjRegistry<RegisteredClientEntity>
    {
        public RegistryClientEntity()
        {
        }

        public RegisteredClientEntity Get<T>() where T : Entity
        {
            return Get(typeof(T).Name);
        }

        protected override void DoRegistration()
        {
            base.DoRegistration();

            var typesWithAttribute = Utility.GetTypesWithAttributeExtended<RegisterClientEntityAttribute>();
            foreach ((Type type, RegisterClientEntityAttribute attr) in typesWithAttribute)
            {
                Register(new RegisteredClientEntity(attr.entityType.Name, attr.entityType));
            }
        }
    }
}
