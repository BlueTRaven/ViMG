using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.VertexDeclarations
{
    public interface IVertexDeclGetters
    {
        Vector3 GetPosition();
        Vector2 GetUV();
        Vector3 GetNormal();

        void SetTangent(Vector3 tangent, Vector3 bitangent);
    }
}
