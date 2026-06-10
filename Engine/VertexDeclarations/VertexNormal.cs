using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.VertexDeclarations
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexNormal : IVertexType
    {
        public static readonly VertexDeclaration VertexDeclaration;

        public short X;
        public short Y;
        public short Z;
        public short W;
        //public Vector4 NormalQuat;
        //public Vector3 Normal;
        //public Vector3 Tangent;
        //public Vector3 Bitangent;

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get
            {
                return VertexDeclaration;
            }
        }

        public static VertexDeclaration NewVertexDeclaration(int offset)
        {
            var elements = new VertexElement[]
            {
                new VertexElement(Marshal.OffsetOf<VertexNormal>("X").ToInt32(), VertexElementFormat.NormalizedShort4, VertexElementUsage.Normal, offset + 0),
                //new VertexElement(Marshal.OffsetOf<VertexNormal>("Normal").ToInt32(), VertexElementFormat.Vector3, VertexElementUsage.Normal, offset + 0),
                //new VertexElement(Marshal.OffsetOf<VertexNormal>("Tangent").ToInt32(), VertexElementFormat.Vector3, VertexElementUsage.Normal, offset + 1),
                //new VertexElement(Marshal.OffsetOf<VertexNormal>("Bitangent").ToInt32(), VertexElementFormat.Vector3, VertexElementUsage.Normal, offset + 2),
            };

            return new VertexDeclaration(elements);
        }

        static VertexNormal()
        {
            VertexDeclaration = NewVertexDeclaration(0);
            //var elements = new VertexElement[]
            //{
            //    new VertexElement(Marshal.OffsetOf<VertexNormal>("Normal").ToInt32(), VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            //    new VertexElement(Marshal.OffsetOf<VertexNormal>("Tangent").ToInt32(), VertexElementFormat.Vector3, VertexElementUsage.Normal, 1),
            //    new VertexElement(Marshal.OffsetOf<VertexNormal>("Bitangent").ToInt32(), VertexElementFormat.Vector3, VertexElementUsage.Normal, 2),
            //};

            //VertexDeclaration = new VertexDeclaration(elements);
        }

        public VertexNormal(Vector3 normal, Vector3 tangent, Vector3 bitangent)
        {
            Matrix mat = new Matrix();
            mat.M11 = normal.X;
            mat.M12 = normal.Y;
            mat.M13 = normal.Z;
            mat.M21 = tangent.X;
            mat.M22 = tangent.Y;
            mat.M23 = tangent.Z;
            mat.M31 = tangent.X;
            mat.M32 = bitangent.Y;
            mat.M33 = bitangent.Z;
            var quat = Quaternion.CreateFromRotationMatrix(mat);
            quat.Normalize();
            if (quat.W < 0) quat = -quat;

            float bias = 1.0f / (float)short.MaxValue;

            if (quat.W < bias)
            {
                float normFactor = MathF.Sqrt(1 - bias * bias);
                quat.W = bias;
                quat.X *= normFactor;
                quat.Y *= normFactor;
                quat.Z *= normFactor;
            }

            Vector3 naturalBinormal = Vector3.Cross(tangent, normal);
            if (Vector3.Dot(naturalBinormal, bitangent) <= 0)
                quat = -quat;

            var vec4 = quat.ToVector4();
            X = (short)(vec4.X * (float)short.MaxValue);
            Y = (short)(vec4.Y * (float)short.MaxValue);
            Z = (short)(vec4.Z * (float)short.MaxValue);
            W = (short)(vec4.W * (float)short.MaxValue);
        }
    }
}
