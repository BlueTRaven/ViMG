using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Entities.Renderers;
using ViMG.Items;
using ViMG.Recipes;
using ViMG.WorldLogics;

namespace Engine.Mods
{
    public abstract class ModRegistryService
    {
        public ItemRegistry? ItemRegistry;
        public CubeRegistry? CubeRegistry;
        public RecipeRegistry? RecipeRegistry;
        public BuffRegistry? BuffRegistry;
        public RendererRegistry? RendererRegistry;
        public WorldLogicRegistry? WorldLogicRegistry;

        public ModRegistryService(GraphicsDevice? device)
        {

        }
    }
}
