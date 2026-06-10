using BrUtility;
using Engine;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Rendering
{
    public static class StaticMaterials
    {
        public static RendererDeferred.DrawMaterial FlatColor = new()
        {
            Diffuse = DrawHelper.WhitePixel,
            Normal = DrawHelper.NormalPixel,
            Emissive = DrawHelper.BlackPixel,
            Specular = DrawHelper.BlackPixel,
        };

        public static RendererDeferred.DrawMaterial Cubes = new("cubes_textures");
        public static RendererDeferred.DrawMaterial CubesWithEmissiveOres = new()
        {
            Diffuse = GlobalState.AssetsManager.GetAsset<Texture2D>("cubes_textures"),
            Normal = GlobalState.AssetsManager.GetAsset<Texture2D>("cubes_textures_normal"),
            Emissive = GlobalState.AssetsManager.GetAsset<Texture2D>("cubes_textures_emissive_ores"),
            Specular = DrawHelper.WhitePixel,
        };

        public static RendererDeferred.DrawMaterial Items = new("swrod");
    }
}
