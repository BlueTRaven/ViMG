using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using BrUtility;

namespace ViMG.Spawners
{
    public abstract class PassiveSpawner
    {
        protected float spawnChance;
        private readonly Rectangle3D spawnBounds;
        protected readonly float spawnRadiusMin;
        protected readonly float spawnRadiusMax;
        protected readonly PassiveSpawnerManager manager;
        private readonly float checkTime;
        private float checkTimer;

        public int SpawnCap { get; protected set; } = 32;

        public PassiveSpawner(PassiveSpawnerManager manager, float checkTime, float spawnChance, Rectangle3D spawnBounds, float spawnRadiusMin = Cube.CUBE_SCALE * 16, float spawnRadiusMax = Cube.CUBE_SCALE * 64)
        {
            if (spawnRadiusMin > MathF.Max(MathF.Max(spawnBounds.Size.X, spawnBounds.Size.Y), spawnBounds.Size.Z))
                throw new Exception("This wouldn't be able to spawn, the min is larger than the size of the rect!");
            this.manager = manager;
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
            if (Main.random.NextDouble() < spawnChance * manager.SpawnChanceMultipler)
            {
                const int MAX_TRIES = 20;
                int tries = MAX_TRIES;

                while (tries > 0)
                {
                    if (GetRandomPosition(world, out CubePosition position))
                    {
                        Chunk spawnChunk = world.GetChunkManager().GetChunk(position);

                        if (spawnChunk != null && CanAreaSpawn(world, world.GetChunkManager(), spawnChunk, position))
                        {
                            Spawn(world, position);
                            break;
                        }
                    }
                    tries--;
                }
            }
        }

        protected virtual bool GetRandomPosition(World world, out CubePosition position)
        {
            float radMin = 0;
            float radMax = MathF.PI * 2;

            /*if (Main.camera is CameraPerspective camera)
            {
                radMin = MathHelper.ToRadians(camera.HalfFOV);
                radMax = MathHelper.ToRadians(360f - camera.HalfFOV);
            }*/

            Vector3 v = -Main.camera.Forward;
            v = Vector3.Transform(v,
                Matrix.CreateFromYawPitchRoll(Main.random.NextFloat(radMin, radMax), Main.random.NextFloat(radMin, radMax), 0));
            v *= Main.random.NextFloat(spawnRadiusMin, spawnRadiusMax);
            v += world.player.Position;

            //TODO clamping to bounds can cause min to no longer be taken into account.
            v = spawnBounds.Clamp(v);

            var cubeAtPos = world.ChunkManager.GetCube(CubePosition.FromWorldSpace(v)).Get();
            if (cubeAtPos == null || cubeAtPos == Main.Registry.CubeRegistry.Air || cubeAtPos.Collision == Cube.CollisionValue.None)
            {
                CubePosition pos = world.ChunkManager.GetFirstSolidDown(CubePosition.FromWorldSpace(v)).GetOrDefault(CubePosition.FromWorldSpace(v));

                position = pos;
                return true;
            }

            position = new CubePosition();
            return false;
        }

        protected abstract void Spawn(World world, CubePosition position);

        public abstract bool CanAreaSpawn(World world, ChunkManager manager, Chunk chunk, CubePosition position);

        public int GetSpawnCap()
        {
            return (int)((float)SpawnCap * manager.SpawnCapMultiplier);
        }
    }
}
