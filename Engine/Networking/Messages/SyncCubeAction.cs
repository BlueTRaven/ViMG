using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Cubes;

namespace Engine.Networking.Messages
{
    public class SyncCubeAction : Message
    {
        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public static SyncCubeAction Instance;

        public struct CubeAction
        {
            public CubePosition position;
            public Cube cube;
            public int playerId;
            public bool leftClicked;
            public bool rightClicked;
        }

        private List<CubeAction> serverActions = new();
        private List<CubeAction> clientActions = new();

        public SyncCubeAction()
        {
            Instance = this;
        }

        public void QueueAction(CubeAction action)
        {
            this.serverActions.Add(action);
        }

        public void DoSend()
        {
            if (serverActions.Count > 0)
            {
                GS.netManagerServer.SendMessageToAll(this, GS.netManagerServer.netManager, null);
                serverActions.Clear();
            }
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;
            netMessage.channel = (int)NetworkMessage.Channels.Entities;
            netMessage.writer.Put(serverActions.Count);

            foreach (var action in serverActions)
            {
                netMessage.writer.Put(action.position);
                netMessage.writer.Put(action.cube.Id);
                netMessage.writer.Put(action.playerId);
                netMessage.writer.Put(action.leftClicked);
                netMessage.writer.Put(action.rightClicked);
            }

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            int num = reader.GetInt();

            for (int i = 0; i < num; i++)
            {
                var position = reader.Get<CubePosition>();
                var cubeId = reader.GetUShort();
                var playerId = reader.GetInt();
                var leftClicked = reader.GetBool();
                var rightClicked = reader.GetBool();

                Cube cube = GlobalState.Registry.CubeRegistry.Get(cubeId) ?? GlobalState.Registry.CubeRegistry.Air;
                if (leftClicked)
                    cube.Client?.OnLeftClick(GS.GetClient(), playerId, position);
                if (rightClicked)
                    cube.Client?.OnRightClick(GS.GetClient(), playerId, position);
            }
        }
    }
}
