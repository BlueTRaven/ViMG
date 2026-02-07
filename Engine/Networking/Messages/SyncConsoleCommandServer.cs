using BrUtility;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.IMGUIImpl;

namespace Engine.Networking.Messages
{
    public class SyncConsoleCommandServer : Message
    {
        public static SyncConsoleCommandServer Instance;

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public SyncConsoleCommandServer()
        {
            Instance = this;
        }

        public void SendCommand(string command, string[] parameters)
        {
            GS.netManagerServer?.SendMessageToAll(Instance, GS.netManagerServer.netManager, new Command
            {
                command = command,
                parameters = parameters,
            });
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);
            netMessage.deliveryMethod = DeliveryMethod.ReliableOrdered;

            Command parameters = addData as Command? ?? throw new ArgumentException("Must be a Command", "addData");

            netMessage.writer.Put(parameters.command);
            netMessage.writer.Put(parameters.parameters.Length);
            foreach (string prm in parameters.parameters)
            {
                netMessage.writer.Put(prm);
            }

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            string commandName = reader.GetString();
            int num = reader.GetInt();
            FastList<string> parameters = new(num);
            for (int i = 0; i < num; i++)
            {
                var parameter = reader.GetString();
                parameters.AddAssumeCapacity(parameter);
            }

            IMGUIConsole.CommandReturn output = IMGUIConsole.RunCommand(commandName, NetworkManager.NetworkSide.Server, parameters.Slice());
        }
    }
}
