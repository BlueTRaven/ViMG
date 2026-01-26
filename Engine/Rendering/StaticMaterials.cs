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
        public static RendererDeferred.DrawMaterial Cubes = new RendererDeferred.DrawMaterial("cubes_textures");
        public static RendererDeferred.DrawMaterial CubesWithEmissiveOres = new RendererDeferred.DrawMaterial()
        {
            Diffuse = GlobalState.assetsManager.GetAsset<Texture2D>("cubes_textures"),
            Normal = GlobalState.assetsManager.GetAsset<Texture2D>("cubes_textures_normal"),
            Emissive = GlobalState.assetsManager.GetAsset<Texture2D>("cubes_textures_emissive_ores"),
            Specular = DrawHelper.WhitePixel,
        };

        public static RendererDeferred.DrawMaterial Items = new RendererDeferred.DrawMaterial("swrod");
    }
}
