using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.IMGUIImpl;

namespace Engine.Networking.Messages
{
    public class SyncConsoleOutput : Message
    {
        public static SyncConsoleOutput Instance;

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public SyncConsoleOutput()
        {
            Instance = this;
        }

        public void SendOutput(string[] output)
        {
            GS.netManagerServer.SendMessageToAll(Instance, GS.netManagerServer.netManager, output);
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);
            netMessage.deliveryMethod = DeliveryMethod.ReliableOrdered;

            string[] response = addData as string[] ?? throw new ArgumentException("Must be string array", "addData");

            netMessage.writer.Put(response.Length);
            foreach (string str in response)
            {
                netMessage.writer.Put(str);
            }

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            var num = reader.GetInt();

            for (int i = 0; i < num; i++)
            {
                string str = reader.GetString();
                IMGUIConsole.LogLine(str);
            }
        }
    }
}
