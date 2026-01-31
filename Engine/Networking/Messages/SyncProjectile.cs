using Engine.Projectiles;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;

namespace Engine.Networking.Messages
{
    public class SyncProjectile : Message
    {
        public static SyncProjectile Instance;

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        private struct ProjectileToSync
        {
            public Common.ProjectileHelper.Projectile projectile;
            public Common.ProjectileHelper.ProjectileStats stats;
            public ProjectileManager.ProjectileReference reference;
            public int visStatsId;
            public bool unload;
        }

        private List<ProjectileToSync> projectilesToSync = new List<ProjectileToSync>();

        public SyncProjectile()
        {
            Instance = this;
        }

        public void Add(ProjectileManager.ProjectileReference reference, Common.ProjectileHelper.Projectile projectile, Common.ProjectileHelper.ProjectileStats stats, int visStatsId)
        {
            projectilesToSync.Add(new ProjectileToSync
            {
                projectile = projectile,
                stats = stats,
                reference = reference,
                visStatsId = visStatsId,
            });
        }

        public void Unload(ProjectileManager.ProjectileReference reference)
        {
            projectilesToSync.Add(new ProjectileToSync
            {
                reference = reference,
                unload = true,
            });
        }

        public void DoSend()
        {
            GS.netManagerServer.SendMessageToAll(Instance, GS.netManagerServer.netManager, null);
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            netMessage.deliveryMethod = LiteNetLib.DeliveryMethod.ReliableUnordered;

            netMessage.writer.Put(projectilesToSync.Count);
            foreach (var projectile in projectilesToSync)
            {
                projectile.reference.Serialize(netMessage.writer);
                netMessage.writer.Put((byte)(projectile.unload ? 1 : 0));
                if (!projectile.unload)
                {
                    netMessage.writer.Put(projectile.projectile.position.X);
                    netMessage.writer.Put(projectile.projectile.position.Y);
                    netMessage.writer.Put(projectile.projectile.position.Z);
                    netMessage.writer.Put(projectile.projectile.velocity.X);
                    netMessage.writer.Put(projectile.projectile.velocity.Y);
                    netMessage.writer.Put(projectile.projectile.velocity.Z);
                    netMessage.writer.Put(projectile.projectile.timeLeft);

                    netMessage.writer.Put(projectile.stats.collisionRadius);
                    netMessage.writer.Put(projectile.stats.dieOnCollision);
                    netMessage.writer.Put(projectile.stats.gravity);
                    netMessage.writer.Put(projectile.stats.gravityScale);
                    netMessage.writer.Put(projectile.stats.size);

                    netMessage.writer.Put(projectile.visStatsId);
                }
                else
                {
                    netMessage.writer.Put(GS.GetWorld().GetTime());
                }
            }

            if (projectilesToSync.Count > 0)
                netMessage.Send();

            projectilesToSync.Clear();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            int num = reader.GetInt();

            for (int i = 0; i < num; i++)
            {
                Common.ProjectileHelper.Projectile projectile;
                Common.ProjectileHelper.ProjectileStats stats;
                int visStatsId;
                ProjectileManager.ProjectileReference reference = ProjectileManager.ProjectileReference.Deserialize(reader);
                bool unload = reader.GetByte() > 0;
                if (!unload)
                {
                    projectile.position.X = reader.GetFloat();
                    projectile.position.Y = reader.GetFloat();
                    projectile.position.Z = reader.GetFloat();
                    projectile.velocity.X = reader.GetFloat();
                    projectile.velocity.Y = reader.GetFloat();
                    projectile.velocity.Z = reader.GetFloat();
                    projectile.timeLeft = reader.GetFloat();

                    stats.collisionRadius = reader.GetFloat();
                    stats.dieOnCollision = reader.GetBool();
                    stats.gravity = reader.GetBool();
                    stats.gravityScale = reader.GetFloat();
                    stats.size = reader.GetFloat();

                    visStatsId = reader.GetInt();
                    //Console.WriteLine("Recv projectile with id {0}:{1}", GlobalState.Registry.ProjectileRegistry.Get(visStatsId).Identifier, visStatsId);

                    GS.GetClient().Current().projectiles.Add(reference, projectile, stats, visStatsId);
                }
                else
                {
                    float time = reader.GetFloat();
                    GS.GetClient().Current().projectiles.Remove(reference, time);
                }
            }
        }
    }
}
