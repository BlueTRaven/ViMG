using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

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
            public int visStatsId;
        }

        private List<ProjectileToSync> projectilesToSync = new List<ProjectileToSync>();

        public SyncProjectile()
        {
            Instance = this;
        }

        public void AddToSync(Common.ProjectileHelper.Projectile projectile, Common.ProjectileHelper.ProjectileStats stats, int visStatsId)
        {
            projectilesToSync.Add(new ProjectileToSync
            {
                projectile = projectile,
                stats = stats,
                visStatsId = visStatsId,
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
                projectile.position.X = reader.GetInt();
                projectile.position.Y = reader.GetInt();
                projectile.position.Z = reader.GetInt();
                projectile.velocity.X = reader.GetInt();
                projectile.velocity.Y = reader.GetInt();
                projectile.velocity.Z = reader.GetInt();
                projectile.timeLeft = reader.GetInt();

                stats.collisionRadius = reader.GetFloat();
                stats.dieOnCollision = reader.GetBool();
                stats.gravity = reader.GetBool();
                stats.gravityScale = reader.GetFloat();
                stats.size = reader.GetFloat();

                visStatsId = reader.GetInt();

                GS.GetClient().Current().projectiles.Add(projectile, stats, visStatsId);
            }
        }
    }
}
