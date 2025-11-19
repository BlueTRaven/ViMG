using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Networking.Messages
{
    public class SyncCubeUpdate : Message
    {
        public static SyncCubeUpdate Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public SyncCubeUpdate()
        {
            Instance = this;
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

            var player = GS.GetWorld().player[playerId];
            GS.GetWorld().ChunkManager.CubeView.SetCube(updatedPos, newId, false);
            GS.GetWorld().ChunkManager.MarkCubeMeshInfoDirty(player, updatedPos, oldId, newId);
        }
    }
}
