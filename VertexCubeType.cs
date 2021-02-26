using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct VertexCubeType : IVertexType
	{
		public Vector3 Position;
		public Vector3 Normal;
		public Vector2 TexCoord;
		public Color Color;

		public static readonly VertexDeclaration VertexDeclaration;

		static VertexCubeType()
		{
			VertexElement[] elements = new VertexElement[] 
			{
				new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
				new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
				new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
				new VertexElement(32, VertexElementFormat.Color, VertexElementUsage.Color, 0)
			};
			VertexDeclaration declaration = new VertexDeclaration(elements);
			VertexDeclaration = declaration;
		}

		public VertexCubeType(Vector3 position, Vector3 normal, Vector2 texCoord, Color color)
		{
			this.Position = position;
			this.Normal = normal;
			this.TexCoord = texCoord;
			this.Color = color;
		}

		VertexDeclaration IVertexType.VertexDeclaration
		{
			get
			{
				return VertexDeclaration;
			}
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = Position.GetHashCode();
				hashCode = (hashCode * 397) ^ Normal.GetHashCode();
				hashCode = (hashCode * 397) ^ TexCoord.GetHashCode();
				hashCode = (hashCode * 397) ^ Color.GetHashCode();
				return hashCode;
			}
		}

	}
}
