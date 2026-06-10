using BrUtility;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.IMGUIImpl;

namespace Engine.Networking.Messages
{
    public struct Command
    {
        public string command;
        public string[] parameters;
    }

    public class SyncConsoleCommandClient : Message
    {
        public static SyncConsoleCommandClient Instance;

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Client;
        
        public SyncConsoleCommandClient()
        {
            Instance = this;
        }

        public void SendCommand(string command, string[] parameters)
        {
            GS.netManagerClient?.SendMessageToAll(Instance, GS.netManagerClient.netManager, new Command
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
            if (output.valid && IMGUIConsole.GetCommandByName(commandName)?.executionSide != ConsoleCommandRunSide.Server)
            {
                SyncConsoleCommandServer.Instance.SendCommand(commandName, parameters.Slice().ToArray());
            }
            else
            {
                SyncConsoleOutput.Instance.SendOutput(output.output);
            }
        }
    }
}
