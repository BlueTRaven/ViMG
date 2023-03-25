using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.VertexDeclarations
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexTransparentPass : IVertexType
    {
        public static readonly VertexDeclaration VertexDeclaration;

        public Vector3 Position;
        public Color Color;
        public Vector2 TextureCoordinate;

        public VertexTransparentPass(VertexCube vertex)
        {
            Position = vertex.Position;
            Color = vertex.Color;
            TextureCoordinate = vertex.TextureCoordinate;
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

        public static bool operator ==(VertexTransparentPass left, VertexTransparentPass right)
        {
            return (((left.Position == right.Position) &&
                (left.Color == right.Color)) &&
                (left.TextureCoordinate == right.TextureCoordinate));
        }

        public static bool operator !=(VertexTransparentPass left, VertexTransparentPass right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj.GetType() != base.GetType())
                return false;

            return (this == ((VertexTransparentPass)obj));
        }

        public override int GetHashCode()
        {
            var hashCode = 2086059520;
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(Position);
            hashCode = hashCode * -1521134295 + EqualityComparer<Color>.Default.GetHashCode(Color);
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector2>.Default.GetHashCode(TextureCoordinate);
            return hashCode;
        }

        static VertexTransparentPass()
        {
            var elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(12 + 4, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            };
            VertexDeclaration = new VertexDeclaration(elements);
        }
    }
}
