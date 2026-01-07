using BepuPhysics.Constraints;
using BrUtility;
using Engine.Clients;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Entities.Renderers
{
    public class RendererDoor : EntityRenderer
    {
        private static VerySimpleMesh mountMesh;
        private static VerySimpleMesh doorMesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("cubes_textures");

        public RendererDoor(GraphicsDevice device) : base("door", device)
        {
            mountMesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE * 0.1f, Cube.CUBE_SCALE * 0.1f, Enums.Alignment.Center);
            doorMesh = MeshHelper.MakeQuad(device, Cube.CUBE_SCALE, Cube.CUBE_SCALE * 2f, Enums.Alignment.Center);
        }

        private static int[]? types = null;
        public override int[] GetRenderedTypes()
        {
            if (types == null)
                types = [Main.Registry.EntityRegistry.Get<Door>().Id];
            return types;
        }

        public override void RenderClientEnt(GraphicsDevice device, double deltaTime, ClientStates client, int type)
        {
            for (int i = 0; i < client.Current().entities.MaxEnts; i++)
            {
                var reference = client.Current().entities.GetReference(i);
                if (client.Current().entities.GetTypeById(reference.id) != type) continue;

                var entCurr = client.Current().entities.GetById(reference.id);
                var entPrev = client.Previous(1).entities.GetById(reference.id);

                var position = entPrev.GetInterpPosition(entCurr);
                var rotation = entPrev.GetInterpRotation(entCurr);
                Main.Renderer.AddOpaqueDraw(new RendererDeferred.GBufferDraw(material, doorMesh,
                    Matrix.CreateFromQuaternion(rotation) *
                    Matrix.CreateTranslation(position), sourceRect: new RectangleF(0, 128, 16, 32)));
            }
            }
    }
}
