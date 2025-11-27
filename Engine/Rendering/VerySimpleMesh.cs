using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.IMGUIImpl;
using ViMG.VertexDeclarations;

namespace ViMG.Rendering
{
    public struct VerySimpleMesh : IDisposable
    {
        public VertexBuffer VBOPosition;
        public VertexBuffer VBOColor;
        public VertexBuffer VBOTexCoord;
        public VertexBuffer VBONormal;
        public VertexBuffer VBOAO;
        public VertexBuffer VBOAnim;

        public IndexBuffer IBO;

        public VertexBufferBinding[] Bindings;

        static class OpaqueVertexDeclarations
        {
            public static VertexDeclaration Position = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0));
            public static VertexDeclaration Color = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Color, VertexElementUsage.Color, 0));
            public static VertexDeclaration TexCoord = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));
            public static VertexDeclaration Normal = VertexNormal.NewVertexDeclaration(0);
            public static VertexDeclaration AO = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1));
            public static VertexDeclaration Animation = VertexAnimated.NewVertexDeclaration(2);
        }

        public static VerySimpleMesh Opaque(GraphicsDevice device, ChunkRenderMesher.VertexAttributes attributes, bool bakeTangents = true)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            VerySimpleMesh mesh = new VerySimpleMesh();
            if (attributes.indices == null || attributes.indices.Count == 0) return mesh;

            if (attributes.position.GetOut(out var positions))
            {
                mesh.VBOPosition = new VertexBuffer(device, OpaqueVertexDeclarations.Position, positions.Length, BufferUsage.WriteOnly);
                mesh.VBOPosition.SetData(positions.Buffer, 0, positions.Length);
            }
            if (attributes.color.GetOut(out var colors))
            {
                mesh.VBOColor = new VertexBuffer(device, OpaqueVertexDeclarations.Color, colors.Length, BufferUsage.WriteOnly);
                mesh.VBOColor.SetData(colors.Buffer, 0, colors.Length);
            }
            if (attributes.texCoord.GetOut(out var texCoords))
            {
                mesh.VBOTexCoord = new VertexBuffer(device, OpaqueVertexDeclarations.TexCoord, texCoords.Length, BufferUsage.WriteOnly);
                mesh.VBOTexCoord.SetData(texCoords.Buffer, 0, texCoords.Length);
            }
            // TODO: normal attribute has a lot of wasted space. All four vertices of the quad are always going to be exactly the same.
            // We could potentially use a structured buffer for this instead, since we could better control what we index.
            // There also might be something in monogame that determines when a vertex buffer index is incremented? Or something.
            if (attributes.normal.GetOut(out var normals))
            {
                if (bakeTangents)
                {
                    IMGUIConsole.Assert(attributes.position.HasValue() && attributes.texCoord.HasValue());

                    for (int i = 0; i < normals.Length; i += 4)
                    {
                        Vector3 tangent = positions[i + 0] - positions[i + 1]; // vert1.GetPosition() - vert2.GetPosition();

                        Vector3 bitangent = Vector3.Cross(positions[i + 0], tangent); // vert1.GetNormal(), tangent);
                        VertexNormal vertex = normals[i];
                        vertex.Tangent = tangent;
                        vertex.Bitangent = bitangent;
                        normals.Buffer[i + 0] = vertex;
                        normals.Buffer[i + 1] = vertex;
                        normals.Buffer[i + 2] = vertex;
                        normals.Buffer[i + 3] = vertex;
                    }
                }
                mesh.VBONormal = new VertexBuffer(device, OpaqueVertexDeclarations.Normal, normals.Length, BufferUsage.WriteOnly);
                mesh.VBONormal.SetData(normals.Buffer, 0, normals.Length);
            }
            if (attributes.ao.GetOut(out var aos))
            {
                mesh.VBOAO = new VertexBuffer(device, OpaqueVertexDeclarations.AO, aos.Length, BufferUsage.WriteOnly);
                mesh.VBOAO.SetData(aos.Buffer, 0, aos.Length);
            }
            if (attributes.animation.GetOut(out var animations))
            {
                mesh.VBOAnim = new VertexBuffer(device, OpaqueVertexDeclarations.Animation, animations.Length, BufferUsage.WriteOnly);
                mesh.VBOAnim.SetData(animations.Buffer, 0, animations.Length);
            }

            mesh.IBO = new IndexBuffer(device, typeof(int), attributes.indices.Count, BufferUsage.WriteOnly);
            mesh.IBO.SetData(attributes.indices.ToArray());
               
            mesh.Bindings = new VertexBufferBinding[]
            {
                new VertexBufferBinding(mesh.VBOPosition, 0),
                new VertexBufferBinding(mesh.VBOColor, 0),
                new VertexBufferBinding(mesh.VBOTexCoord, 0),
                new VertexBufferBinding(mesh.VBONormal, 0),
                new VertexBufferBinding(mesh.VBOAO, 0),
                new VertexBufferBinding(mesh.VBOAnim, 0),
            };

            return mesh;
        }

        static class TransparentVertexDeclarations
        {
            public static VertexDeclaration Position = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0));
            public static VertexDeclaration Color = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Color, 0));
            public static VertexDeclaration TexCoord = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));
        }

        public static VerySimpleMesh Transparent(GraphicsDevice device, ChunkRenderMesher.VertexAttributes attributes)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            VerySimpleMesh mesh = new VerySimpleMesh();
            if (attributes.indices == null || attributes.indices.Count == 0) return mesh;

            if (attributes.position.GetOut(out var positions))
            {
                mesh.VBOPosition = new VertexBuffer(device, OpaqueVertexDeclarations.Position, positions.Length, BufferUsage.WriteOnly);
                mesh.VBOPosition.SetData(positions.Buffer, 0, positions.Length);
            }
            if (attributes.color.GetOut(out var colors))
            {
                mesh.VBOColor = new VertexBuffer(device, OpaqueVertexDeclarations.Color, colors.Length, BufferUsage.WriteOnly);
                mesh.VBOColor.SetData(colors.Buffer, 0, colors.Length);
            }
            if (attributes.texCoord.GetOut(out var texCoords))
            {
                mesh.VBOTexCoord = new VertexBuffer(device, OpaqueVertexDeclarations.TexCoord, texCoords.Length, BufferUsage.WriteOnly);
                mesh.VBOTexCoord.SetData(texCoords.Buffer, 0, texCoords.Length);
            }

            mesh.IBO = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, attributes.indices.Count, BufferUsage.WriteOnly);
            mesh.IBO.SetData(attributes.indices.ToArray());

            mesh.Bindings = new VertexBufferBinding[]
            {
                new VertexBufferBinding(mesh.VBOPosition, 0),
                new VertexBufferBinding(mesh.VBOColor, 0),
                new VertexBufferBinding(mesh.VBOTexCoord, 0),
            };

            return mesh;
        }

        static class ShadowVertexDeclarations
        {
            public static VertexDeclaration Position = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0));
            public static VertexDeclaration TexCoord = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));
        }

        public static VerySimpleMesh Shadow(GraphicsDevice device, ChunkRenderMesher.VertexAttributes attributes)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            VerySimpleMesh mesh = new VerySimpleMesh();
            if (attributes.indices == null || attributes.indices.Count == 0) return mesh;

            if (attributes.position.GetOut(out var positions))
            {
                mesh.VBOPosition = new VertexBuffer(device, OpaqueVertexDeclarations.Position, positions.Length, BufferUsage.WriteOnly);
                mesh.VBOPosition.SetData(positions.Buffer, 0, positions.Length);
            }
            if (attributes.texCoord.GetOut(out var texCoords))
            {
                mesh.VBOTexCoord = new VertexBuffer(device, OpaqueVertexDeclarations.TexCoord, texCoords.Length, BufferUsage.WriteOnly);
                mesh.VBOTexCoord.SetData(texCoords.Buffer, 0, texCoords.Length);
            }

            mesh.IBO = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, attributes.indices.Count, BufferUsage.WriteOnly);
            mesh.IBO.SetData(attributes.indices.ToArray());

            mesh.Bindings = new VertexBufferBinding[]
            {
                new VertexBufferBinding(mesh.VBOPosition, 0),
                new VertexBufferBinding(mesh.VBOTexCoord, 0),
            };

            return mesh;
        }

        static class SolidColorVertexDeclarations
        {
            public static VertexDeclaration Position = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0));
            public static VertexDeclaration Color = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Color, 0));
        }

        public static VerySimpleMesh SolidColor(GraphicsDevice device, ChunkRenderMesher.VertexAttributes attributes)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            VerySimpleMesh mesh = new VerySimpleMesh();
            // FIXME: for some reason, air meshes are getting passed in with some indices, but no vertices. The attributes.position.Get()== null is to catch that.
            // This is a bug, it should be fixed at the root eventually.
            if (attributes.indices == null || attributes.indices.Count == 0 || attributes.position.Get() == null) return mesh;

            if (attributes.position.GetOut(out var positions))
            {
                mesh.VBOPosition = new VertexBuffer(device, OpaqueVertexDeclarations.Position, positions.Length, BufferUsage.WriteOnly);
                mesh.VBOPosition.SetData(positions.Buffer, 0, positions.Length);
            }
            if (attributes.color.GetOut(out var colors))
            {
                mesh.VBOColor = new VertexBuffer(device, OpaqueVertexDeclarations.Color, colors.Length, BufferUsage.WriteOnly);
                mesh.VBOColor.SetData(colors.Buffer, 0, colors.Length);
            }

            mesh.IBO = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, attributes.indices.Count, BufferUsage.WriteOnly);
            mesh.IBO.SetData(attributes.indices.ToArray());

            mesh.Bindings = new VertexBufferBinding[]
            {
                new VertexBufferBinding(mesh.VBOPosition, 0),
                new VertexBufferBinding(mesh.VBOColor, 0),
            };

            return mesh;
        }

        public void Dispose()
        {
            VBOPosition?.Dispose();
            VBOColor?.Dispose();
            VBOTexCoord?.Dispose();
            VBONormal?.Dispose();
            VBOAO?.Dispose();
            VBOAnim?.Dispose();
            IBO?.Dispose();
        }
    }
}
