using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;

namespace ViMG.Spawners
{
    public class PassiveSpawnerManager
    {
        private List<PassiveSpawner> spawners = new List<PassiveSpawner>();

        public float SpawnCapMultiplier = 1;
        public float SpawnChanceMultipler = 1;

        public PassiveSpawnerManager(EntityManager entityManager)
        {
        }

        public void AddPassiveSpawner(PassiveSpawner spawner)
        {
            spawners.Add(spawner);
        }

        public void Update(double deltaTime, World world)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            spawners.ForEach(x => x.Update(deltaTime, world));

            SpawnCapMultiplier = 1f;
            SpawnChanceMultipler = 1f;
        }
    }
}
