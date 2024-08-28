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
    public struct VertexNormal : IVertexType
    {
        public static readonly VertexDeclaration VertexDeclaration;

        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector3 Bitangent;

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
                new VertexElement(Marshal.OffsetOf<VertexNormal>("Normal").ToInt32(), VertexElementFormat.Vector3, VertexElementUsage.Normal, offset + 0),
                new VertexElement(Marshal.OffsetOf<VertexNormal>("Tangent").ToInt32(), VertexElementFormat.Vector3, VertexElementUsage.Normal, offset + 1),
                new VertexElement(Marshal.OffsetOf<VertexNormal>("Bitangent").ToInt32(), VertexElementFormat.Vector3, VertexElementUsage.Normal, offset + 2),
            };

            return new VertexDeclaration(elements);
        }

        static VertexNormal()
        {
            var elements = new VertexElement[]
            {
                new VertexElement(Marshal.OffsetOf<VertexNormal>("Normal").ToInt32(), VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(Marshal.OffsetOf<VertexNormal>("Tangent").ToInt32(), VertexElementFormat.Vector3, VertexElementUsage.Normal, 1),
                new VertexElement(Marshal.OffsetOf<VertexNormal>("Bitangent").ToInt32(), VertexElementFormat.Vector3, VertexElementUsage.Normal, 2),
            };

            VertexDeclaration = new VertexDeclaration(elements);
        }
    }
}
