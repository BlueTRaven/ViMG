using BrAssetsManager;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Generation;

namespace ViMG.AssetHandlers
{
    public class AssetHandlerStructure : AssetHandler
    {
        public AssetHandlerStructure(ContentManager contentManager, AssetsManager assetManager) : base(".struct", "Structures", contentManager, assetManager)
        {
        }

		public override void LoadAsset(string key)
		{
			assets[key].loadedAsset = true;
			byte[] bytes = File.ReadAllBytes("Content/" + assets[key].fileName + extentionName);
			assets[key].asset = Structure.Deserialize(bytes);
		}
	}
}
