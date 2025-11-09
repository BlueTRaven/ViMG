using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
	public struct WorldViewProjection
	{
		private Matrix world;
		private Matrix view;
		private Matrix projection;

		private bool isWorldDirty;
		private bool isViewDirty;
		private bool isProjDirty;

		private Matrix calculatedMatrix;

		public void SetWorld(Matrix world)
		{
			this.world = world;
			isWorldDirty = true;
		}

		public void SetView(Matrix view)
		{
			this.view = view;
			isViewDirty = true;
		}

		public void SetProjection(Matrix projection)
		{
			this.projection = projection;
			isProjDirty = true;
		}

		public Matrix GetWorld()
        {
			return world;
        }

		public Matrix GetView()
        {
			return view;
        }

		public Matrix GetProjection()
        {
			return projection;
        }

		public Matrix Get()
		{
			if (isWorldDirty || isViewDirty || isProjDirty)
			{
				calculatedMatrix = world * view * projection;
			}

			return calculatedMatrix;
		}
	}
}
