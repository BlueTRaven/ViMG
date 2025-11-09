using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Generation;

namespace ViMG.WorldLogics
{
    public class WorldLogicRegistry
    {
        public int maxLayers = 0;

        public Type[] logics;
        public Type[] generators;

        public virtual void RegisterAll() 
        {
            var maxLayers = GetMaxLayers();
            Array.Resize(ref logics, maxLayers);
            Array.Resize(ref generators, maxLayers);
        }

        protected virtual int GetMaxLayers() 
        {
            return maxLayers;
        }

        public void AddFromOther(WorldLogicRegistry? other)
        {
            if (other != null)
            {
                maxLayers = int.Max(other.GetMaxLayers(), GetMaxLayers());

                Array.Resize(ref logics, maxLayers);
                Array.Resize(ref generators, maxLayers);

                for (int i = 0; i < maxLayers; i++)
                {
                    // TODO error/warning aobut overwriting logics
                    this.logics[i] = other.logics[i];
                    this.generators[i] = other.generators[i];
                }
            }
        }
    }
}
