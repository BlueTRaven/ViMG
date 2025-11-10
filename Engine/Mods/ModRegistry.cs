using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Mods
{
    public class ModRegistry : ObjRegistry<Mod>
    {
        private readonly GraphicsDevice? device;

        private Dictionary<Assembly, int> assemblyToMod = new();

        public ModRegistry(GraphicsDevice? device)
        {
            this.device = device;
        }

        protected override void DoRegistration()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetCustomAttribute<Engine.Mods.ModAssemblyAttribute>() != null)
                {
                    Type[] allTypes = assembly.GetTypes();

                    foreach (Type type in allTypes)
                    {
                        if (type.IsSubclassOf(typeof(Mod)))
                        {
                            Mod created = (Mod)Activator.CreateInstance(type);
                            Register(created);
                            assemblyToMod.Add(assembly, Count);
                        }
                    }
                }
            }
        }

        public void AddSpawnInventoryItems(Inventory inventory)
        {
            foreach (Mod mod in this.GetIterable())
            {
                mod.AddSpawnInventoryItems(inventory);
            }
        }

        public Mod Get(Assembly assembly)
        {
            return Get(assemblyToMod[assembly]);
        }
    }
}
