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
    public record struct VertexCube : IVertexDeclGetters
    {
        public Vector3 Position;
        public Color Color;
        public Vector2 TextureCoordinate;
        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector3 Bitangent;
        public float AO;

        public float AnimFrameTime;
        public float NumAnimFrames;
        public float AnimFrameSize;

        public VertexCube(Vector3 position, Color color, Vector2 textureCoordinate, Vector3 normal)
        {
            Position = position;
            Color = color;
            TextureCoordinate = textureCoordinate;
            Normal = normal;
            AO = 1;

            AnimFrameTime = 0;
            NumAnimFrames = 0;
            AnimFrameSize = 0;
        }

        public override string ToString()
        {
            return "{{Position:" + Position + " Color:" + Color + " TextureCoordinate:" + TextureCoordinate + "}}";
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
            this.Tangent = tangent;
            this.Bitangent = bitangent;
        }
    }
}
