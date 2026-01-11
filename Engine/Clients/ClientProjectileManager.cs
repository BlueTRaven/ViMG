using BrUtility;
using Engine.ChunkStuff;
using Engine.Clients.Entities;
using Engine.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        private ProjectileHelper.Projectile[] projectiles = new ProjectileHelper.Projectile[ProjectileManager.PROJECTILES_MAX];
        private ProjectileHelper.ProjectileStats[] stats = new ProjectileHelper.ProjectileStats[ProjectileManager.PROJECTILES_MAX];
        private int[] visStatIds = new int[ProjectileManager.PROJECTILES_MAX];
        private bool[] active = new bool[ProjectileManager.PROJECTILES_MAX];

        private FastList<int> freeList = new FastList<int>();

        public ClientProjectileManager(GraphicsDevice device)
        {
            if (mesh.IBO == null)
                mesh = MeshHelper.MakeQuad(device, 1, 1, Enums.Alignment.Center);

            for (int i = ProjectileManager.PROJECTILES_MAX - 1; i >= 0; i--)
            {
                freeList.Add(i);
            }
        }

        public void NewFrame(ClientProjectileManager prev, double deltaTime)
        {
            for (int i = 0; i < prev.projectiles.Length; i++)
            {
                projectiles[i] = prev.projectiles[i];
                stats[i] = prev.stats[i];
                visStatIds[i] = prev.visStatIds[i];
                active[i] = prev.active[i];
            }
        }

        public void Update(ICubeGetter cubeView, double deltaTime)
        {
            for (int i = 0; i < projectiles.Length; i++)
            {
                if (active[i])
                {
                    if (!ProjectileHelper.UpdateProjectile(ref projectiles[i], ref stats[i], cubeView, deltaTime))
                    {
                        active[i] = false;
                        freeList.Add(i);
                    }
                }
            }
        }

        public ProjectileHelper.Projectile GetProjectile(int id)
        {
            return projectiles[id];
        }

        public bool GetActive(int id)
        {
            return active[id];
        }

        public ProjectileManager.ProjectileVisStats GetVisStats(int id)
        {
            return Main.Registry.ProjectileRegistry.Get(visStatIds[id])?.VisStats() ?? new();
        }

        public static void Render(GraphicsDevice device, ClientStates client)
        {
            for (int i = 0; i < ProjectileManager.PROJECTILES_MAX; i++)
            {
                if (client.Current().projectiles.GetActive(i))
                {
                    var visStats = client.Current().projectiles.GetVisStats(i);
                    ProjectileHelper.Projectile pprev = client.Previous(1).projectiles.GetProjectile(i);
                    ProjectileHelper.Projectile pcurr = client.Current().projectiles.GetProjectile(i);
                    ProjectileHelper.Projectile projectile = new ProjectileHelper.Projectile 
                    {
                        position = Vector3.Lerp(pprev.position, pcurr.position, (float)Main.TimeC),
                        velocity = Vector3.Lerp(pprev.velocity, pcurr.velocity, (float)Main.TimeC),
                        timeLeft = float.Lerp(pprev.timeLeft, pcurr.timeLeft, (float)Main.TimeC),
                    };

                    if (!visStats.rollFollowsVelocity)
                    {
                        Main.Renderer.AddOpaqueDraw(new RendererDeferred.GBufferDraw(material, mesh,
                            Matrix.CreateScale(visStats.scale) *
                            Matrix.CreateFromQuaternion(client.InterpCamera.Rotation) *
                            Matrix.CreateTranslation(projectile.position), visStats.sourceRect));
                    }
                    else
                    {
                        Vector3 axis = projectile.velocity;
                        axis.Normalize();

                        Matrix mat = Matrix.CreateConstrainedBillboard(projectile.position,
                            client.InterpCamera.Position, axis, -client.InterpCamera.Forward, Vector3.Forward);

                        Main.Renderer.AddOpaqueDraw(new RendererDeferred.GBufferDraw(material, mesh,
                            Matrix.CreateScale(visStats.scale) *
                            mat, visStats.sourceRect));
                    }
                }
            }
        }

        public void Add(ProjectileHelper.Projectile projectile, ProjectileHelper.ProjectileStats stats, int visStatsId)
        {
            if (freeList.Length > 0)
            {
                int index = freeList.Length - 1;
                freeList.RemoveAt(freeList.Length - 1);

                projectiles[index] = projectile;
                this.stats[index] = stats;
                visStatIds[index] = visStatsId;
                active[index] = true;
            }
        }
    }
}
