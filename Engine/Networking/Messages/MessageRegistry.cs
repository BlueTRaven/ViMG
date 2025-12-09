using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.IMGUIImpl;

namespace Engine.Networking.Messages
{
    public class MessageRegistry : ObjRegistry<Message>
    {
        protected override void DoRegistration()
        {
            base.DoRegistration();

            Register(new SyncPlayerConnected());
            Register(new SyncChunk());
            Register(new SyncBasicState());
            Register(new SyncEntityState());
            Register(new SyncEntityStateAck());
            Register(new WhoAmI());
            Register(new WhoAmIRequest());
            Register(new SyncPlayerInputs());
            Register(new SyncCubeUpdate());
            Register(new SyncCubeUpdateAuditRequest());
            Register(new SyncCubeUpdateAuditResponse());
            Register(new SyncInventoryUpdate());
            Register(new SyncInventoryUpdateAuditRequest());
            Register(new SyncInventoryUpdateAuditResponse());
            Register(new SyncInventoryInput());
            Register(new SyncWorldState());
        }

        public override void Register(Message obj)
        {
            obj.Id = Count + 1;
            base.Register(obj);
        }

        public void SendMessageToPeer(Message message, NetPeer peer, object? addData)
        {
            if (Main.gameStateManager.netMode == ViMG.GameStates.GameStateManager.NetworkingMode.Server)
            {
                IMGUIConsole.Assert((message.SendableFrom & NetworkManager.NetworkSide.Server) == NetworkManager.NetworkSide.Server);
            } 
            else
            {
                IMGUIConsole.Assert((message.SendableFrom & NetworkManager.NetworkSide.Client) == NetworkManager.NetworkSide.Client);
            }

            NetworkMessage netMessage = new NetworkMessage(message.Id, Main.gameStateManager.TheIsland.netManager.netManager, peer);
            netMessage.writer.Put(message.Id);

            message.SendMessage(netMessage, addData);
        }

        public void SendMessageToAll(Message message, NetManager netManager, object? addData, NetPeer? excludePeer = null)
        {
            NetworkMessage netMessage = new NetworkMessage(message.Id, netManager, null);
            netMessage.excludePeer = excludePeer;
            netMessage.writer.Put(message.Id);

            message.SendMessage(netMessage, addData);

            //Console.WriteLine("Send message {0} to all excluding {1}", message.GetType().Name, excludePeer?.ToString());
        }

        public void Dispatch(NetManager netManager, NetPacketReader reader, NetPeer source, byte channel, DeliveryMethod deliveryMethod)
        {
            int position = reader.Position;
            int messageType = reader.GetInt();

            if (Main.gameStateManager.netMode == ViMG.GameStates.GameStateManager.NetworkingMode.Server && Get(messageType).Passthrough)
            {
                reader.SetPosition(position);
                var allBytes = reader.GetRemainingBytes();
                reader.SetPosition(position);
                reader.GetInt();
                netManager.SendToAll(allBytes, channel, deliveryMethod, source);
            }
            Get(messageType).ReceiveMessage(reader, source);
        }
    }
}
