using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Rendering;

namespace ViMG.Entities.Renderers
{
    public static class StaticMaterials
    {
        public static RendererDeferred.DrawMaterial Cubes = new RendererDeferred.DrawMaterial("cubes_textures");
        public static RendererDeferred.DrawMaterial CubesWithEmissiveOres = new RendererDeferred.DrawMaterial()
        {
            Diffuse = Main.assetsManager.GetAsset<Texture2D>("cubes_textures"),
            Normal = Main.assetsManager.GetAsset<Texture2D>("cubes_textures_normal"),
            Emissive = Main.assetsManager.GetAsset<Texture2D>("cubes_textures_emissive_ores"),
            Specular = DrawHelper.WhitePixel,
        };

        public static RendererDeferred.DrawMaterial Items = new RendererDeferred.DrawMaterial("swrod");
    }
}
