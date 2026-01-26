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
    public class CubeBreakProgressTracker
    {
        public struct BreakProgress
        {
            public CubePosition position;
            public ChunkPosition chunk;
            public float timer;
            public int progress;    //goes up one per "mine"
        }

        private Dictionary<CubePosition, BreakProgress> tracked = new Dictionary<CubePosition, BreakProgress>();
        private List<CubePosition> toRemoveLater = new List<CubePosition>();
        private List<BreakProgress> toUpdateLater = new List<BreakProgress>();

        public void Update(ICubeGetter cubeView, double deltaTime)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

            foreach (var mined in tracked)
            {
                BreakProgress mc = mined.Value;

                Cube cube = cubeView.GetCube(mc.position).GetOrDefault(GlobalState.Registry.CubeRegistry.Air);

                mc.timer -= (float)deltaTime;
                if (mc.timer <= 0)
                {
                    mc.progress--;
                    mc.timer = 2;
                }

                if (mc.progress <= 0 || cube == GlobalState.Registry.CubeRegistry.Air)
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

        public IEnumerable<BreakProgress> GetIter()
        {
            return tracked.Values;
        }

        public ushort GetProgress(CubePosition position)
        {
            return (ushort)tracked[position].progress;
        }

        public bool AddProgress(ICubeGetter cubeView, CubePosition position, int progress)
        {
            BreakProgress curProgress = new()
            {
                position = position,
                chunk = ChunkPosition.CubeChunk(position),
                progress = progress,
                timer = 2,
            };

            if (tracked.TryGetValue(position, out var foundProgress))
            {
                foundProgress.progress += progress;
                foundProgress.timer = 2;
                curProgress = foundProgress;
            }

            Cube cube = cubeView.GetCube(position).GetOrDefault(GlobalState.Registry.CubeRegistry.Air);

            if (curProgress.progress >= cube.MineProgressToBreak)
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
            BreakProgress curProgress = new()
            {
                position = position,
                chunk = ChunkPosition.CubeChunk(position),
                progress = progress,
                timer = 2,
            };

            if (tracked.TryGetValue(position, out var foundProgress))
            {
                foundProgress.progress = progress;
                foundProgress.timer = 2;
                curProgress = foundProgress;
            }

            Cube cube = cubeView.GetCube(position).GetOrDefault(GlobalState.Registry.CubeRegistry.Air);

            if (curProgress.progress >= cube.MineProgressToBreak)
            {
                tracked.Remove(position);
                return true;
            }

            tracked[position] = curProgress;
            return false;
        }
    }
}
