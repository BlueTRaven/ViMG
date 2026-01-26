using BepuPhysics.Constraints;
using Engine.ChunkStuff;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;

namespace Engine.Common
{
    public static class ProjectileHelper
    {
        public struct Projectile
        {
            public Vector3 position;
            public Vector3 velocity;
            public float timeLeft;
        }

        public struct ProjectileStats
        {
            public float collisionRadius;
            public float size;
            public bool gravity;
            public float gravityScale;
            public bool dieOnCollision;
        }

        public static bool UpdateProjectile(ref Projectile projectile, ref readonly ProjectileStats stats, ICubeGetter cubeView, double deltaTime)
        {
            Span<CubePosition> positions = stackalloc CubePosition[3 * 3 * 3];
            Span<ushort> ids = stackalloc ushort[3 * 3 * 3];
            int pi = 0;

            projectile.timeLeft -= (float)deltaTime;

            if (projectile.timeLeft <= 0)
            {
                return false;
            }

            if (stats.gravity)
            {
                projectile.velocity.Y += World.GRAVITY * stats.gravityScale;

                if (projectile.velocity.Y < -340)
                    projectile.velocity.Y = -340;
            }

            projectile.position += projectile.velocity * (float)deltaTime;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        CubePosition pos = CubePosition.FromWorldSpace(projectile.position) +
                            new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace);

                        if (cubeView.IsInBounds(pos))
                        {
                            positions[pi] = pos;
                            pi++;
                        }
                    }
                }
            }

            cubeView.GetIds(positions[..pi], ids[..pi]);

            for (int j = 0; j < 3 * 3 * 3; j++)
            {
                CubePosition pos = positions[j];
                ushort id = ids[j];

                if (GlobalState.Registry.CubeRegistry.GetOrDefault(id, GlobalState.Registry.CubeRegistry.Air).Solid)
                {
                    if (CollisionHelper.CheckCollision(CubePosition.BoundsWorldSpace(pos), projectile.position,
                                                        stats.collisionRadius, out Vector3 change))
                    {
                        if (stats.dieOnCollision && change.Length() > 0)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}
