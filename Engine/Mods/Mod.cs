using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Mods
{
    public abstract class Mod : IRegisterable
    {
        public abstract string Identifier { get; }
        public abstract ModRegistryService Registry { get; }

        public virtual void OnRegister()
        {

        }

        public abstract ModRegistryService CreateModRegistryService(GraphicsDevice device);

        public virtual void AddSpawnInventoryItems(Inventory inventory) { }
    }
}
