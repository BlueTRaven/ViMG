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
            //Register(new SyncInventoryUpdateAuditRequest());
            //Register(new SyncInventoryUpdateAuditResponse());
            Register(new SyncInventoryInput());
            Register(new SyncWorldState());
            Register(new SyncInventory());
            Register(new SyncInventoryAck());
            Register(new SyncCubeAction());
            Register(new SyncProjectile());
        }

        public override void Register(Message obj)
        {
            obj.Id = Count + 1;
            base.Register(obj);
        }

        public void Dispatch(NetManager netManager, NetPacketReader reader, NetPeer source, byte channel, DeliveryMethod deliveryMethod)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

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

            using (var zoneGetMessage = ViMG.TracyImpl.Tracy.BeginZone(name: "ReceiveMessage"))
            {
                Get(messageType).ReceiveMessage(reader, source);
            }
        }
    }
}
