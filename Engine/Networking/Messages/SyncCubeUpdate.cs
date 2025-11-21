using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Networking.Messages
{
    public class SyncCubeUpdate : Message
    {
        public static SyncCubeUpdate Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        private struct QueuedCubeUpdated
        {
            public int player;
            public CubePosition position;
            public ushort oldId, newId;
            public double time;
        }
        private List<QueuedCubeUpdated> queued1 = new();
        private List<QueuedCubeUpdated> queued2 = new();
        private List<QueuedCubeUpdated> queued;

        public SyncCubeUpdate()
        {
            Instance = this;

            queued = queued1;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            ChunkManager.CubeUpdated cubeUpdated = addData as ChunkManager.CubeUpdated? ?? throw new Exception();
            Debug.Assert(cubeUpdated.updated == cubeUpdated.notified);

            netMessage.writer.Put(cubeUpdated.timeUpdated);
            netMessage.writer.Put(cubeUpdated.player?.playerIndex ?? -1);
            netMessage.writer.Put(cubeUpdated.updated);
            netMessage.writer.Put(cubeUpdated.oldId);
            netMessage.writer.Put(cubeUpdated.newId);

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            double time = reader.GetDouble();
            int playerId = reader.GetInt();
            CubePosition a = reader.Get<CubePosition>();
            CubePosition updatedPos = new CubePosition(a.X, a.Y, a.Z, CubePosition.CoordinateSpace.CubeSpace);
            ushort oldId = reader.GetUShort();
            ushort newId = reader.GetUShort();

            queued.Add(new QueuedCubeUpdated
            {
                oldId = oldId,
                newId = newId,
                position = updatedPos,
                player = playerId,
                time = time,
            });
        }

        public void Apply(ChunkManager chunkManager, Player?[] players)
        {
            var otherBuffer = queued == queued1 ? queued2 : queued1;

            foreach (QueuedCubeUpdated qcubeupdated in queued)
            {
                if (Main.Time > qcubeupdated.time)
                {
                    var player = qcubeupdated.player == -1 ? null : players[qcubeupdated.player];
                    chunkManager.CubeView.SetCube(qcubeupdated.position, qcubeupdated.newId, false);
                    chunkManager.MarkCubeMeshInfoDirty(player, qcubeupdated.position, qcubeupdated.oldId, qcubeupdated.newId);
                    chunkManager.ChunkMesher?.MarkChunkDirty(ChunkPosition.CubeChunk(qcubeupdated.position));
                }
                else
                {
                    otherBuffer.Add(qcubeupdated);
                }
            }

            queued.Clear();
            queued = otherBuffer;
        }
    }
}
