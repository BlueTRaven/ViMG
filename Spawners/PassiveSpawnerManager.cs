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
            if (Main.ENABLE_ENT_SPAWNING)
            {
                spawners.Add(new PSSlime(this, entityManager));
                spawners.Add(new PSSKeleton(this, entityManager));
                spawners.Add(new PSImp(this, entityManager));
                spawners.Add(new PSCaveSlime(this, entityManager));
                spawners.Add(new PSSnake(this, entityManager));
                spawners.Add(new PSStoneBeetle(this, entityManager));
            }
        }

        public void Update(double deltaTime, World world)
        {
            spawners.ForEach(x => x.Update(deltaTime, world));

            SpawnCapMultiplier = 1f;
            SpawnChanceMultipler = 1f;
        }
    }
}
