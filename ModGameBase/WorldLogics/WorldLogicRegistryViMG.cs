using Engine.Clients.WorldLogics;
using Engine.WorldLogics;
using ModGameBase.Client.WorldLogics;
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
            return 256;
        }


        public override void RegisterAll()
        {
            base.RegisterAll();

            logics[0] = typeof(WorldLogicIsland);
            logics[1] = typeof(WorldLogicCatacombs);
            logics[255] = typeof(WorldLogicNone);

            clientLogics[0] = typeof(ClientWorldLogicIsland);
            clientLogics[1] = typeof(ClientWorldLogic);
            clientLogics[255] = typeof(ClientWorldLogic);

            generators[0] = typeof(ChunkGeneratorIsland);
            generators[1] = typeof(ChunkGeneratorCatacombs);
            generators[255] = typeof(ChunkGeneratorFlat);
        }

    }
}
