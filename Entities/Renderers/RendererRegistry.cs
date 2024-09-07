using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Entities.Renderers
{
    public class RendererRegistry : ObjRegistry<EntityRenderer>
    {
        private Dictionary<Type, EntityRenderer> objsByType = new Dictionary<Type, EntityRenderer>();

        private readonly GraphicsDevice device;

        public RendererRegistry(GraphicsDevice device)
        {
            this.device = device;
        }

        protected override void DoRegistration()
        {
            base.DoRegistration();

            Register(new RendererTree(device));
            Register(new RendererDoor(device));
            Register(new RendererOpaqueBillboardedEntity(device));
            Register(new RendererManaStar(device));
            Register(new RendererSkullheadEye(device));
            Register(new RendererSkullhead(device));
        }

        protected override void Register(EntityRenderer obj)
        {
            base.Register(obj);

            objsByType.Add(obj.GetType(), obj);
        }
    }
}
