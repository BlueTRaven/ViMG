using Microsoft.Xna.Framework.Graphics;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.VertexDeclarations
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexAnimated : IVertexType
    {
        public static readonly VertexDeclaration VertexDeclaration;

        public float AnimFrameTime;
        public short NumAnimFrames;
        public short AnimFrameSize;

        public VertexAnimated(float animFrameTime, float numAnimFrames, float animFrameSize)
        {
            this.AnimFrameTime = animFrameTime;
            this.NumAnimFrames = (short)(numAnimFrames * (float)short.MaxValue);
            this.AnimFrameSize = (short)(animFrameSize * (float)short.MaxValue);
        }

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
                new VertexElement(Marshal.OffsetOf<VertexAnimated>("AnimFrameTime").ToInt32(), VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, offset + 0),
                new VertexElement(Marshal.OffsetOf<VertexAnimated>("NumAnimFrames").ToInt32(), VertexElementFormat.NormalizedShort2, VertexElementUsage.TextureCoordinate, offset + 1),
                //new VertexElement(Marshal.OffsetOf<VertexAnimated>("AnimFrameSize").ToInt32(), VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, offset + 2),
            };

            return new VertexDeclaration(elements);
        }

        static VertexAnimated()
        {
            var elements = new VertexElement[]
            {
                new VertexElement(Marshal.OffsetOf<VertexAnimated>("AnimFrameTime").ToInt32(), VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(Marshal.OffsetOf<VertexAnimated>("NumAnimFrames").ToInt32(), VertexElementFormat.NormalizedShort2, VertexElementUsage.TextureCoordinate, 1),
            };

            VertexDeclaration = new VertexDeclaration(elements);
        }
    }
}
