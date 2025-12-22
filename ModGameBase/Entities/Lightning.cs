using BrUtility;
using Engine;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Entities
{
    public class Lightning : Entity, ISyncBasicState
    {
        public const float SPLIT_DISTANCE = Cube.CUBE_SCALE * 4f;
        public static Color LightningColor = new Color(255, 253, 141);

        private Vector3 bottomPosition;

        public Vector3[] positions;

        private int light = -1;

        private float timer;
        private int seed;

        public Lightning(Vector3 position)
        {
            this.Position = position;
            AlwaysRender = true;

            timer = 5f / 60f;
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            bottomPosition = world.ChunkManager.CubeView.GetFirstSolidDown(CubePosition.FromWorldSpace(Position)).Get().InWorldSpaceCenter();

            Vector3 direction = bottomPosition - Position;
            float distance = direction.Length();
            direction.Normalize();

            int numSplits = (int)(distance / SPLIT_DISTANCE);

            positions = new Vector3[numSplits + 1];

            seed = random.Next();

            for (int i = 0; i < numSplits; i++)
            {
                PCG32 pcg = new PCG32((ulong)(seed + i));

                positions[i] = Position + direction * SPLIT_DISTANCE * (i + 1);
                positions[i] += new Vector3(pcg.NextFloat(-Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 2f), 0, pcg.NextFloat(-Cube.CUBE_SCALE * 2f, Cube.CUBE_SCALE * 2f));
            }

            positions[numSplits] = bottomPosition;

            light = world.LightManager.Add(bottomPosition, Cube.CUBE_SCALE * 4, Cube.CUBE_SCALE * 8, LightningColor.ToVector4(), false);
        }

        public override void OnUnload()
        {
            base.OnUnload();

            if (light != -1)
                world.LightManager.Remove(light);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            timer -= (float)deltaTime;

            if (timer <= 0)
                world.EntityManager.Kill(this);
        }

        public void Get(out BasicState state)
        {
            state = new BasicState
            {
                position = Position,
                velocity = bottomPosition,
                timers = { [0] = timer },
                counters = { [0] = seed },
            };
        }

        public void Set(ref readonly BasicState state)
        {
            Position = state.position;
            bottomPosition = state.velocity;
            timer = state.timers[0];
            seed = state.counters[0];
        }
    }
}
