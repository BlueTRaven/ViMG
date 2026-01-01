using BrUtility;
using Engine.Clients;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Entities.Renderers
{
    //Draws the local player
    public class RendererPlayer : EntityRenderer
    {
        private VerySimpleMesh mesh;

        public RendererPlayer(GraphicsDevice device) : base("player_local", device)
        {
            mesh = MeshHelper.MakeQuad(device, 1, 0.98f * 2f, Enums.Alignment.Center);
        }

        private static int[]? types = null;
        public override int[] GetRenderedTypes()
        {
            if (types == null)
                types = [Main.Registry.EntityRegistry.Get<Player>().Id];
            return types;
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex, List<Entity> renderedType)
        {
            return;
        }

        public override void RenderClientEnt(GraphicsDevice device, double deltaTime, ClientStates client, int type)
        {

        }
    }
}
