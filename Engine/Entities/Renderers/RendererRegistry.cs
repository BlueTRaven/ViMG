using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities.Renderers;
using ViMG.Items;

namespace Engine.Entities.Renderers
{
    public class RendererRegistry : ObjRegistry<EntityRenderer>
    {
        private Dictionary<Type, EntityRenderer> objsByType = new Dictionary<Type, EntityRenderer>();

        protected readonly GraphicsDevice device;

        public RendererRegistry(GraphicsDevice device)
        {
            this.device = device;
        }

        protected override void DoRegistration()
        {
            base.DoRegistration();

            Register(new RendererPlayer(device));
            Register(new RendererOpaqueBillboardedEntity(device));
            Register(new RendererOpaqueXMeshEntity(device));
            Register(new RendererLine(device));
            Register(new RendererGenericExplosion(device));
            Register(new RendererEntityItem(device));
        }

        public override void Register(EntityRenderer obj)
        {
            base.Register(obj);

            objsByType.Add(obj.GetType(), obj);
        }
    }
}
