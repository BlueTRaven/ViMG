using BrUtility;
using Engine.Clients;
using Engine.Clients.Entities;
using Engine.Entities;
using Engine.Items;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Entities.Renderers;
using ViMG.Rendering;
using ViMG.VertexDeclarations;

namespace Engine.Entities.Renderers
{
    //Draws the local player
    public class RendererPlayer : EntityRenderer
    {
        private VerySimpleMesh mesh;
        private VerySimpleMesh lookAtMesh;

        public RendererPlayer(GraphicsDevice device) : base("player_local", device)
        {
            mesh = MeshHelper.MakeQuad(device, 1, 0.98f * 2f, Enums.Alignment.Center);

            FastList<VertexCube> vertices = new FastList<VertexCube>();
            List<int> indices = new List<int>();
            MeshHelper.MakeCubeVertsVertexPositionColorTextureNormal(Vector3.Zero, new Vector3(Cube.CUBE_SCALE), MeshHelper.CubeFace.ALL, Color.White, vertices, indices);
            lookAtMesh = VerySimpleMesh.Transparent(device, ChunkRenderMesher.VertexAttributes.Transparent(vertices, indices));
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
            for (int i = 0; i < EntityManager.EntMax; i++)
            {
                var reference = client.Current().entities.GetReference(i);
                if (client.Current().entities.GetTypeById(reference.id) != type) continue;

                var entCurr = client.Current().entities.GetById(reference.id);
                var entPrev = client.Previous(1).entities.GetById(reference.id);

                var extraState = entCurr.GetExtra<Player.PlayerExtraState>();
                Inventory? inventory = client.inventoryManager.Get(extraState.inventory);
                if (inventory != null)
                {
                    var invItem = inventory.Get(extraState.highlightIndex);

                    invItem.item?.DrawInHand(device, inventory.Get(extraState.highlightIndex), entCurr, -BasicState.Forward(ref entCurr));
                }
            }
        }
    }
}
