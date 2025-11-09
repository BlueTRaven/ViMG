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
    public struct VertexEmptyPass : IVertexType, IVertexDeclGetters
    {
        public static readonly VertexDeclaration VertexDeclaration;

        public Vector3 Position;
        public Color Color;

        public VertexEmptyPass(VertexCube vertex)
        {
            Position = vertex.Position;
            Color = vertex.Color;
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
            return "{{Position:" + this.Position + "}}";
        }

        public static bool operator ==(VertexEmptyPass left, VertexEmptyPass right)
        {
            return (left.Position == right.Position);
        }

        public static bool operator !=(VertexEmptyPass left, VertexEmptyPass right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj.GetType() != base.GetType())
                return false;

            return (this == ((VertexEmptyPass)obj));
        }

        public override int GetHashCode()
        {
            var hashCode = 2086059520;
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(Position);
            hashCode = hashCode * -1521134295 + EqualityComparer<Color>.Default.GetHashCode(Color);
            return hashCode;
        }

        public Vector3 GetPosition()
        {
            return Position;
        }

        public Vector2 GetUV()
        {
            return Vector2.Zero;
        }

        public Vector3 GetNormal()
        {
            return Vector3.Zero;
        }

        public void SetTangent(Vector3 tangent, Vector3 bitangent)
        {
        }

        static VertexEmptyPass()
        {
            var elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            };
            VertexDeclaration = new VertexDeclaration(elements);
        }
    }
}
