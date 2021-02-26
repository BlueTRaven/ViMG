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
	public struct VertexPositionColorTextureNormal : IVertexType
	{
		public static readonly VertexDeclaration VertexDeclaration;

		public Vector3 Position;
		public Color Color;
		public Vector2 TextureCoordinate;
		public Vector3 Normal;

		public VertexPositionColorTextureNormal(Vector3 position, Color color, Vector2 textureCoordinate, Vector3 normal)
		{
			Position = position;
			Color = color;
			TextureCoordinate = textureCoordinate;
			Normal = normal;
		}

		VertexDeclaration IVertexType.VertexDeclaration
		{
			get
			{
				return VertexDeclaration;
			}
		}

		public override string ToString()
		{
			return "{{Position:" + this.Position + " Color:" + this.Color + " TextureCoordinate:" + this.TextureCoordinate + "}}";
		}

		public static bool operator ==(VertexPositionColorTextureNormal left, VertexPositionColorTextureNormal right)
		{
			return (((left.Position == right.Position) && (left.Color == right.Color)) && (left.TextureCoordinate == right.TextureCoordinate));
		}

		public static bool operator !=(VertexPositionColorTextureNormal left, VertexPositionColorTextureNormal right)
		{
			return !(left == right);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			if (obj.GetType() != base.GetType())
				return false;

			return (this == ((VertexPositionColorTextureNormal)obj));
		}

		public override int GetHashCode()
		{
			var hashCode = 2086059520;
			hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(Position);
			hashCode = hashCode * -1521134295 + EqualityComparer<Color>.Default.GetHashCode(Color);
			hashCode = hashCode * -1521134295 + EqualityComparer<Vector2>.Default.GetHashCode(TextureCoordinate);
			hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(Normal);
			return hashCode;
		}

		static VertexPositionColorTextureNormal()
		{
			int sizeVec4 = Marshal.SizeOf<Vector4>();
			int sizeVec3 = Marshal.SizeOf<Vector3>();
			int sizeColor = Marshal.SizeOf<Color>();
			int sizeVec2 = Marshal.SizeOf<Vector2>();

			int elem1 = sizeVec3;
			int elem2 = sizeVec3 + sizeColor;
			int elem3 = sizeVec3 + sizeColor + sizeVec2;

			var elements = new VertexElement[]
			{
				new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
				new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
				new VertexElement(12 + 4, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
				new VertexElement(12 + 4 + 8, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
			};
			VertexDeclaration = new VertexDeclaration(elements);
		}
	}
}
