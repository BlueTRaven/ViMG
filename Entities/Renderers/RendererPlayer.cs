using BrUtility;
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
    //Draws the local player
    public class RendererPlayer : EntityRenderer
    {
        private VerySimpleMesh mesh;

        public RendererPlayer(GraphicsDevice device) : base("player_local", device)
        {
            mesh = MeshHelper.MakeQuad(device, 1, 0.98f * 2f, Enums.Alignment.Center);
        }

        private static Type[] types = [typeof(Player)];
        public override Type[] GetRenderedTypes()
        {
            return types;
        }

        public override void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex, List<Entity> renderedEntities)
        {
            var player = renderedEntities[0] as Player;

            if (player.inventory.Get(player.menuPlayer.HighlightIndex).item != null)
            {
                player.inventory.Get(player.menuPlayer.HighlightIndex).item.DrawInHand(device, player.inventory.Get(player.menuPlayer.HighlightIndex), player, -Main.camera.Forward);
            }
        }
    }
}
