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
    public struct VertexOpaquePass : IVertexType, IVertexDeclGetters
    {
        public static readonly VertexDeclaration VertexDeclaration;

        public Vector3 Position;
        public Color Color;
        public Vector2 TextureCoordinate;
        public Vector3 Normal;
        public Vector4 NormalQuat;
        public float AO;

        public float AnimFrameTime;
        public float NumAnimFrames;
        public float AnimFrameSize;

        public VertexOpaquePass(VertexCube vertex)
        {
            Position = vertex.Position;
            Color = vertex.Color;
            TextureCoordinate = vertex.TextureCoordinate;
            Normal = vertex.Normal;
            NormalQuat = Vector4.Zero;
            AO = vertex.AO;

            AnimFrameTime = vertex.AnimFrameTime;
            NumAnimFrames = vertex.NumAnimFrames;
            AnimFrameSize = vertex.AnimFrameSize;
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

        public static bool operator ==(VertexOpaquePass left, VertexOpaquePass right)
        {
            return (((left.Position == right.Position) &&
                (left.Color == right.Color)) &&
                (left.TextureCoordinate == right.TextureCoordinate));
        }

        public static bool operator !=(VertexOpaquePass left, VertexOpaquePass right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj.GetType() != base.GetType())
                return false;

            return (this == ((VertexOpaquePass)obj));
        }

        public override int GetHashCode()
        {
            var hashCode = 2086059520;
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(Position);
            hashCode = hashCode * -1521134295 + EqualityComparer<Color>.Default.GetHashCode(Color);
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector2>.Default.GetHashCode(TextureCoordinate);
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(Normal);
            hashCode = hashCode * -1521134295 + EqualityComparer<float>.Default.GetHashCode(AO);
            return hashCode;
        }

        public Vector3 GetPosition()
        {
            return Position;
        }

        public Vector2 GetUV()
        {
            return TextureCoordinate;
        }

        public Vector3 GetNormal()
        {
            return Normal;
        }

        public void SetTangent(Vector3 tangent, Vector3 bitangent)
        {
            Matrix mat = new Matrix();
            var a = new BepuUtilities.Matrix3x3();
            a.X = GetNormal().ToNumerics();
            a.Y = tangent.ToNumerics();
            a.Z = bitangent.ToNumerics();
            mat.M11 = a.X.X;
            mat.M21 = a.X.Y;
            mat.M31 = a.X.Z;
            mat.M12 = a.Y.X;
            mat.M22 = a.Y.Y;
            mat.M32 = a.Y.Z;
            mat.M13 = a.Z.X;
            mat.M23 = a.Z.Y;
            mat.M33 = a.Z.Z;
            var quat = Quaternion.CreateFromRotationMatrix(mat);

            NormalQuat = quat.ToVector4();
        }

        static VertexOpaquePass()
        {
            var elements = new VertexElement[]
            {
                new VertexElement(Marshal.OffsetOf(typeof(VertexOpaquePass), "Position").ToInt32(), VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(Marshal.OffsetOf(typeof(VertexOpaquePass), "Color").ToInt32(), VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(Marshal.OffsetOf(typeof(VertexOpaquePass), "TextureCoordinate").ToInt32(), VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(Marshal.OffsetOf(typeof(VertexOpaquePass), "Normal").ToInt32(), VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(Marshal.OffsetOf(typeof(VertexOpaquePass), "NormalQuat").ToInt32(), VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
                new VertexElement(Marshal.OffsetOf(typeof(VertexOpaquePass), "AO").ToInt32(), VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 2),
                new VertexElement(Marshal.OffsetOf(typeof(VertexOpaquePass), "AnimFrameTime").ToInt32(), VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 3),
                new VertexElement(Marshal.OffsetOf(typeof(VertexOpaquePass), "NumAnimFrames").ToInt32(), VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 4),
                new VertexElement(Marshal.OffsetOf(typeof(VertexOpaquePass), "AnimFrameSize").ToInt32(), VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 5),
                
                /*new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(12 + 4, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(12 + 4 + 8, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(12 + 4 + 8 + 12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 1),
                new VertexElement(12 + 4 + 8 + 12 + 12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 2),
                new VertexElement(12 + 4 + 8 + 12 + 12 + 12, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1),
                new VertexElement(12 + 4 + 8 + 12 + 12 + 12 + 4, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 2),
                new VertexElement(12 + 4 + 8 + 12 + 12 + 12 + 4 + 4, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 3),
                new VertexElement(12 + 4 + 8 + 12 + 12 + 12 + 4 + 4 + 4, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 4),*/
            };
            VertexDeclaration = new VertexDeclaration(elements);
        }
    }
}
