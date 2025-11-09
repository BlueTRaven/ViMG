using Engine.Mods;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace ModGameBase
{
    public class Class1 : Mod
    {
        public override string Identifier => "test";

        public override ModRegistryService Registry => null;

        public override ModRegistryService CreateModRegistryService(GraphicsDevice device)
        {
            return null;
        }

        public override void OnRegister()
        {
            base.OnRegister();

            Console.WriteLine("I LOADED");
        }
    }
}
