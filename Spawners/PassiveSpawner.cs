using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Spawners
{
    public abstract class PassiveSpawner
    {
        protected float spawnChance;
        private readonly Rectangle3D spawnBounds;
        protected readonly float spawnRadiusMin;
        protected readonly float spawnRadiusMax;
        private readonly float checkTime;
        private float checkTimer;

        public PassiveSpawner(float checkTime, float spawnChance, Rectangle3D spawnBounds, float spawnRadiusMin = Cube.CUBE_SCALE * 16, float spawnRadiusMax = Cube.CUBE_SCALE * 64)
        {
            if (spawnRadiusMin > MathF.Max(MathF.Max(spawnBounds.Size.X, spawnBounds.Size.Y), spawnBounds.Size.Z))
                throw new Exception("This wouldn't be able to spawn, the min is larger than the size of the rect!");

            this.checkTime = checkTime;
            this.checkTimer = checkTime;

            this.spawnChance = spawnChance;
            this.spawnBounds = spawnBounds;
            this.spawnRadiusMin = spawnRadiusMin;
            this.spawnRadiusMax = spawnRadiusMax;
        }

        public virtual void Initialize(World world)
        {

        }

        public virtual void Update(double deltaTime, World world)
        {
            checkTimer -= (float)deltaTime;

            if (checkTimer <= 0)
            {
                checkTimer = checkTime;

                DoSpawnCheck(world);
            }
        }

        protected void DoSpawnCheck(World world)
        {
            if (Main.random.NextDouble() < spawnChance)
            {
                Vector3 v = new Vector3(1, 0, 0);
                v = Vector3.Transform(v,
                    Matrix.CreateRotationX((float)Main.random.NextDouble() * MathHelper.Pi * 2) *
                    Matrix.CreateRotationY((float)Main.random.NextDouble() * MathHelper.Pi * 2) *
                    Matrix.CreateRotationZ((float)Main.random.NextDouble() * MathHelper.Pi * 2));
                v *= (float)Main.random.NextDouble() * (spawnRadiusMax - spawnRadiusMin) + spawnRadiusMin;
                v += world.player.Position;

                //TODO clamping to bounds can cause min to no longer be taken into account.
                v = spawnBounds.Clamp(v);

                var cubeAtPos = world.ChunkManager.GetCube(CubePosition.FromWorldSpace(v)).Get();
                if (cubeAtPos == null || cubeAtPos == Main.Registry.CubeRegistry.Air || cubeAtPos.Collision == Cube.CollisionValue.None)
                {
                    CubePosition pos = world.ChunkManager.GetFirstSolidDown(CubePosition.FromWorldSpace(v)).GetOrDefault(CubePosition.FromWorldSpace(v));
                    Chunk spawnChunk = world.GetChunkManager().GetChunk(pos);
                    if (spawnChunk != null && CanAreaSpawn(world, world.GetChunkManager(), world.GetChunkManager().GetChunk(pos), pos))
                        Spawn(world, pos);
                }
            }
        }

        protected abstract void Spawn(World world, CubePosition position);

        public abstract bool CanAreaSpawn(World world, ChunkManager manager, Chunk chunk, CubePosition position);
    }
}
