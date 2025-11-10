using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.WorldLogics
{
    public abstract class WorldLogic : IDisposable
    {
        public WorldLogic()
        {

        }

        public virtual void Initialize(World world)
        {

        }

        public virtual void FinishLoading(GraphicsDevice device) 
        {

        }

        public virtual void Update(World world, double deltaTime)
        {

        }

        public virtual void OnCubeUpdated(CubePosition updating, int updatedId)
        {

        }

        public virtual bool AllowsLoadingNextLayer(World world)
        {
            return true;
        }

        public virtual void Draw(World world, GraphicsDevice device)
        {

        }

        public virtual void Dispose()
        {
        }
    }
}
