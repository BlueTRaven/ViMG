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
	public struct VertexCube : IVertexType
	{
		public static readonly VertexDeclaration VertexDeclaration;

		public Vector3 Position;
		public Color Color;
		public Vector2 TextureCoordinate;
		public SharpDX.Half4 NormalAO;

		public float AnimFrameTime;
		public float NumAnimFrames;
		public float AnimFrameSize;

		public VertexCube(Vector3 position, Color color, Vector2 textureCoordinate, Vector3 normal)
		{
			Position = position;
			Color = color;
			TextureCoordinate = textureCoordinate;
			NormalAO = new SharpDX.Half4(normal.X, normal.Y, normal.Z, 1);

			AnimFrameTime = 0;
			NumAnimFrames = 0;
			AnimFrameSize = 0;
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

		public static bool operator ==(VertexCube left, VertexCube right)
		{
			return (((left.Position == right.Position) && 
				(left.Color == right.Color)) && 
				(left.TextureCoordinate == right.TextureCoordinate));
		}

		public static bool operator !=(VertexCube left, VertexCube right)
		{
			return !(left == right);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			if (obj.GetType() != base.GetType())
				return false;

			return (this == ((VertexCube)obj));
		}

		public override int GetHashCode()
		{
			var hashCode = 2086059520;
			hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(Position);
			hashCode = hashCode * -1521134295 + EqualityComparer<Color>.Default.GetHashCode(Color);
			hashCode = hashCode * -1521134295 + EqualityComparer<Vector2>.Default.GetHashCode(TextureCoordinate);
			hashCode = hashCode * -1521134295 + EqualityComparer<SharpDX.Half4>.Default.GetHashCode(NormalAO);
			return hashCode;
		}

		static VertexCube()
		{
			var elements = new VertexElement[]
			{
				new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
				new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
				new VertexElement(12 + 4, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
				new VertexElement(12 + 4 + 8, VertexElementFormat.HalfVector4, VertexElementUsage.Normal, 0),
				new VertexElement(12 + 4 + 8 + 8, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1),
				new VertexElement(12 + 4 + 8 + 8 + 4, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 2),
				new VertexElement(12 + 4 + 8 + 8 + 4 + 4, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 3),
			};
			VertexDeclaration = new VertexDeclaration(elements);
		}
	}
}
