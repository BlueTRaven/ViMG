using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DemoRenderer.ShapeDrawing
{
    [StructLayout(LayoutKind.Explicit, Size = 48)]
    public struct BoxInstance
    {
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(12)]
        public uint PackedColor;
        [FieldOffset(16)]
        public Quaternion Orientation;
        [FieldOffset(32)]
        public float HalfWidth;
        [FieldOffset(36)]
        public float HalfHeight;
        [FieldOffset(40)]
        public float HalfLength;
    }

    //This isn't exactly an efficient representation, but isolated triangle rendering is mainly just for small scale testing anyway.
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct TriangleInstance
    {
        [FieldOffset(0)]
        public Vector3 A;
        [FieldOffset(12)]
        public uint PackedColor;
        [FieldOffset(16)]
        public Vector3 B;
        [FieldOffset(28)]
        public float X;
        [FieldOffset(32)]
        public Vector3 C;
        [FieldOffset(44)]
        public float Y;
        [FieldOffset(48)]
        public ulong PackedOrientation;
        [FieldOffset(56)]
        public float Z;
    }

    [StructLayout(LayoutKind.Explicit, Size = 48)]
    public struct MeshInstance
    {
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(12)]
        public uint PackedColor;
        [FieldOffset(16)]
        public ulong PackedOrientation;
        [FieldOffset(24)]
        public int VertexStart;
        [FieldOffset(28)]
        public int VertexCount;
        [FieldOffset(32)]
        public Vector3 Scale;
    }
}

