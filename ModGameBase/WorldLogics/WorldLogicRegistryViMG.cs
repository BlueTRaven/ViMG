using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Generation;

namespace ViMG.WorldLogics
{
    public class WorldLogicRegistryViMG : WorldLogicRegistry
    {
        protected override int GetMaxLayers()
        {
            return 2;
        }


        public override void RegisterAll()
        {
            base.RegisterAll();

            logics[0] = typeof(WorldLogicIsland);
            logics[1] = typeof(WorldLogicCatacombs);

            generators[0] = typeof(ChunkGeneratorIsland);
            generators[1] = typeof(ChunkGeneratorCatacombs);
        }

    }
}
