using BrUtility;
using Engine.ChunkStuff;
using Engine.Clients.Entities;
using Engine.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;
using ViMG.Rendering;

namespace Engine.Clients
{
    public class ClientProjectileManager
    {
        private static VerySimpleMesh mesh;
        private static RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("projectiles");

        private struct ProjectileHolder
        {
            public ProjectileHelper.Projectile projectile;
            public ProjectileHelper.ProjectileStats stats;
            public int visStatsId;

            public ProjectileManager.ProjectileReference reference;

            public bool active;
        }

        private double time;
        private ProjectileHolder[] projectiles = new ProjectileHolder[ProjectileManager.ProjMax];

        public ClientProjectileManager()
        {
        }

        public void NewFrame(ClientProjectileManager prev, double deltaTime, double time)
        {
            for (int i = 0; i < prev.projectiles.Length; i++)
            {
                projectiles[i] = prev.projectiles[i];
            }

            this.time = time;
        }

        public void Update(ICubeGetter cubeView, double deltaTime)
        {
            for (int i = 0; i < projectiles.Length; i++)
            {
                if (projectiles[i].active)
                {
                    if (!ProjectileHelper.UpdateProjectile(ref projectiles[i].projectile, ref projectiles[i].stats, cubeView, deltaTime))
                    {
                        projectiles[i].reference = projectiles[i].reference.NextGeneration();
                        projectiles[i].active = false;
                    }
                }
            }
        }

        public ProjectileHelper.Projectile GetProjectile(int id)
        {
            return projectiles[id].projectile;
        }

        public ProjectileManager.ProjectileReference GetReference(int id)
        {
            return projectiles[id].reference;
        }

        public bool IsActive(int id)
        {
            return projectiles[id].active;
        }

        public ProjectileManager.ProjectileVisStats GetVisStats(int id)
        {
            return GlobalState.Registry.ProjectileRegistry.Get(projectiles[id].visStatsId)?.VisStats() ?? new();
        }

        public static void Render(GraphicsDevice device, ClientStates client)
        {
            if (mesh.IBO == null)
                mesh = MeshHelper.MakeQuad(device, 1, 1, Enums.Alignment.Center);

            for (int i = 0; i < ProjectileManager.ProjMax; i++)
            {
                if (client.Current().projectiles.IsActive(i))
                {
                    var visStats = client.currInterpState.projectiles.GetVisStats(i);
                    ProjectileHelper.Projectile projectile = client.currInterpState.projectiles.GetProjectile(i);

                    if (visStats.hasLight)
                    {
                        client.LightManager.AddShadowmapped(new LightManager2.LightConfig
                        {
                            position = projectile.position,
                            min = visStats.lightExtents.X,
                            max = visStats.lightExtents.Y,
                            color = visStats.lightColor,
                        });
                    }

                    if (!visStats.rollFollowsVelocity)
                    {
                        client.Renderer.AddOpaqueDraw(new RendererDeferred.GBufferDraw(material, mesh,
                            Matrix.CreateScale(visStats.scale) *
                            Matrix.CreateFromQuaternion(-client.currInterpState.camera.Rotation) *
                            Matrix.CreateTranslation(projectile.position), visStats.sourceRect));
                    }
                    else
                    {
                        Vector3 axis = projectile.velocity;
                        axis.Normalize();

                        Matrix mat = Matrix.CreateConstrainedBillboard(projectile.position,
                            client.currInterpState.camera.Position, axis, -client.currInterpState.camera.Forward, Vector3.Forward);

                        client.Renderer.AddOpaqueDraw(new RendererDeferred.GBufferDraw(material, mesh,
                            Matrix.CreateScale(visStats.scale) *
                            mat, visStats.sourceRect));
                    }
                }
            }
        }

        public void Add(ProjectileManager.ProjectileReference reference, ProjectileHelper.Projectile projectile, ProjectileHelper.ProjectileStats stats, int visStatsId)
        {
            int index = reference.id;
            projectiles[index] = new ProjectileHolder
            {
                projectile = projectile,
                stats = stats,
                visStatsId = visStatsId,
                reference = reference,
                active = true,
            };
        }

        public void Set(ProjectileManager.ProjectileReference reference, ProjectileHelper.Projectile projectile)
        {
            int index = reference.id;
            projectiles[index] = projectiles[index] with
            {
                projectile = projectile,
            };
        }

        public void Remove(ProjectileManager.ProjectileReference reference, float atTime)
        {
            if (projectiles[reference.id].reference.generation != reference.generation)
                return;

            projectiles[reference.id].projectile.timeLeft = (float)(atTime - time);
        }
    }
}
