using Engine.IMGUIImpl;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Win32;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.IMGUIImpl;
using ViMG.TracyImpl;

namespace Engine.Networking.Messages
{
    public class MessageRegistry : ObjRegistry<Message>
    {
        protected override void DoRegistration()
        {
            base.DoRegistration();

            Register(new SyncPlayerConnected());
            Register(new SyncChunk());
            Register(new SyncEntityState());
            Register(new SyncEntityStateAck());
            Register(new WhoAmI());
            Register(new WhoAmIRequest());
            Register(new SyncPlayerInputs());
            Register(new SyncCubeUpdate());
            //Register(new SyncCubeUpdateAuditRequest());
            //Register(new SyncCubeUpdateAuditResponse());
            Register(new SyncInventoryUpdate());
            //Register(new SyncInventoryUpdateAuditRequest());
            //Register(new SyncInventoryUpdateAuditResponse());
            Register(new SyncInventoryInput());
            Register(new SyncWorldState());
            Register(new SyncInventory());
            Register(new SyncInventoryAck());
            Register(new SyncCubeAction());
            Register(new SyncProjectile());
            Register(new SyncConsoleCommandClient());
            Register(new SyncConsoleCommandServer());
            Register(new SyncConsoleOutput());
            Register(new SyncChatMessageClient());
            Register(new SyncChatMessageServer());
            Register(new SyncPlayerStats());
        }

        public override void Register(Message obj)
        {
            obj.Id = Count + 1;
            base.Register(obj);
        }

        public void Dispatch(NetworkManager netManager, NetPacketReader reader, NetPeer source, byte channel, DeliveryMethod deliveryMethod)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

            int position = reader.Position;
            int messageType = reader.GetInt();

            IMGUINetworkDebug.AddMessage(new IMGUINetworkDebug.NetworkDebugMessage
            {
                messageType = messageType,
                time = DateTime.Now,
                isServer = netManager.IsServer,
            });
            //Console.WriteLine("[{0}] Recv message {1} at {2}", netManager.IsServer ? "Server" : "Client", Get(messageType)?.Identifier, DateTime.Now);
            if (netManager.IsServer && Get(messageType).Passthrough)
            {
                reader.SetPosition(position);
                var allBytes = reader.GetRemainingBytes();
                reader.SetPosition(position);
                reader.GetInt();
                netManager.netManager.SendToAll(allBytes, channel, deliveryMethod, source);
            }

            using (var zoneGetMessage = ViMG.TracyImpl.Tracy.BeginZone(name: "ReceiveMessage"))
            {
                var message = Get(messageType);
                ViMG.TracyImpl.Tracy.EmitMessage(string.Format("[{0}] Recv Message {1}", netManager.IsServer ? "Server" : "Client", message.GetType().FullName));
                message.ReceiveMessage(reader, source);
            }
        }
    }
}
