using BrAssetsManager;
using BrNineSlice;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.AssetHandlers;
using ViMG.Generation;

namespace ViMG
{
	public class ViMGAssetsManager : AssetsManager
	{
		public ViMGAssetsManager(ContentManager contentManager) : base(contentManager)
		{
		}

		public override void AddAssetTypes(ContentManager content)
		{
			assetTypes.Add(typeof(Texture2D), new AssetHandlerTexture2D(content, this));
			assetTypes.Add(typeof(SpriteFont), new AssetHandlerFont(content, this));
			assetTypes.Add(typeof(Model), new AssetHandlerModel(content, this));
			assetTypes.Add(typeof(Effect), new AssetHandlerEffect(content, this));
			assetTypes.Add(typeof(NineSlice), new AssetHandlerNineSlice(content, this));
			assetTypes.Add(typeof(Structure), new AssetHandlerStructure(content, this));
		}

		public override void LoadContent(string fulldirectoryname)
		{
			base.LoadContent(fulldirectoryname);
		}
	}
}
