using Engine.ChunkStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Cubes;

namespace Engine.Common
{
    public class CubeProgressTracker
    {
        private struct ProgressCube
        {
            public CubePosition position;
            public ChunkPosition chunk;
            public float timer;
            public int progress;    //goes up one per "mine"
        }

        private Dictionary<CubePosition, ProgressCube> tracked = new Dictionary<CubePosition, ProgressCube>();
        private List<CubePosition> toRemoveLater = new List<CubePosition>();
        private List<ProgressCube> toUpdateLater = new List<ProgressCube>();

        public void Update(ICubeGetter cubeView, double deltaTime)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

            foreach (var mined in tracked)
            {
                ProgressCube mc = mined.Value;

                Cube cube = cubeView.GetCube(mc.position).GetOrDefault(Main.Registry.CubeRegistry.Air);

                mc.timer -= (float)deltaTime;
                if (mc.timer <= 0)
                {
                    mc.progress--;
                    mc.timer = 2;
                }

                if (mc.progress <= 0 || cube == Main.Registry.CubeRegistry.Air)
                    toRemoveLater.Add(mc.position);
                else toUpdateLater.Add(mc);
            }

            foreach (var pos in toRemoveLater)
            {
                tracked.Remove(pos);
            }

            foreach (var mc in toUpdateLater)
            {
                tracked[mc.position] = mc;
            }

            toRemoveLater.Clear();
            toUpdateLater.Clear();
        }

        public ushort GetProgress(CubePosition position)
        {
            return (ushort)tracked[position].progress;
        }

        public bool AddProgress(ICubeGetter cubeView, CubePosition position, int progress)
        {
            ProgressCube curProgress = new()
            {
                position = position,
                chunk = ChunkPosition.CubeChunk(position),
                progress = progress,
                timer = 2,
            };

            if (tracked.TryGetValue(position, out curProgress))
            {
                curProgress.progress += progress;
            }

            Cube cube = cubeView.GetCube(position).GetOrDefault(Main.Registry.CubeRegistry.Air);

            if (curProgress.progress > cube.MineProgressToBreak)
            {
                tracked.Remove(position);
                return true;
            }

            tracked[position] = curProgress;
            return false;
        }

        public void RemoveProgress(CubePosition position)
        {
            tracked.Remove(position);
        }

        public bool SetProgress(ICubeGetter cubeView, CubePosition position, int progress)
        {
            ProgressCube curProgress = new()
            {
                position = position,
                chunk = ChunkPosition.CubeChunk(position),
                progress = progress,
                timer = 2,
            };

            if (tracked.TryGetValue(position, out curProgress))
            {
                curProgress.progress = progress;
            }

            Cube cube = cubeView.GetCube(position).GetOrDefault(Main.Registry.CubeRegistry.Air);

            if (curProgress.progress > cube.MineProgressToBreak)
            {
                tracked.Remove(position);
                return true;
            }

            tracked[position] = curProgress;
            return false;
        }
    }
}
