using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Cubes;
using ViMG.Entities;
using static HexaGen.Runtime.MemoryPool;

namespace ModGameBase.Entities
{
    public struct IdleStats
    {
        public float idleTimer;
        public float idleMoveTimer;
        public int idleMovements;
        public Vector2 idleDirection;
        public Vector2 idleHome;

        public void Update(Random rand, Vector3 position, double deltaTime)
        {
            idleTimer -= (float)deltaTime;

            if (idleTimer <= 0)
                idleMoveTimer -= (float)deltaTime;

            if (idleMovements == 0 && idleTimer <= 0 && idleMoveTimer <= 0)
            {
                idleHome = new Vector2(position.X, position.Z);

                idleTimer = rand.NextFloat(4f, 12f);
                idleMoveTimer = rand.NextFloat(0.25f, 2f);
                idleMovements = rand.Next(2, 6);

                idleDirection = rand.NextAngle();
            }
            else
            {
                float distFromIdleHome = (new Vector2(position.X, position.Z) - idleHome).Length();

                if (distFromIdleHome > Cube.CUBES_PER_UNIT * 16)
                    idleDirection = -idleDirection;

                if (idleTimer <= 0 && idleMoveTimer <= 0)
                {
                    idleMovements--;
                    idleDirection = rand.NextAngle();
                    idleMoveTimer = rand.NextFloat(0.25f, 2f);
                }
            }
        }

        public void OnSave(List<byte> bytes)
        {
            SaveHelper.SaveVector2(bytes, idleDirection);
            SaveHelper.SaveVector2(bytes, idleHome);
        }

        public void OnLoad(byte[] bytes, ref int index)
        {
            idleDirection = SaveHelper.LoadVector2(bytes, ref index);
            idleHome = SaveHelper.LoadVector2(bytes, ref index);
        }
    }
}
