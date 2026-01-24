using Engine.IMGUIImpl;
using LiteNetLib;
using SharpDX.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.WorldLogics;

namespace Engine.Networking.Messages
{
    public class SyncWorldState : Message
    {
        public static SyncWorldState Instance { get; private set; }
        
        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        // if we're running locally then we only have one instance of a Message class!
        // have to use different fields...
        public int ServerSequence;
        public int ClientSequence;

        public SyncWorldState()
        {
            Instance = this;
        }

        public void DoSend()
        {
            GS.netManagerServer?.SendMessageToAll(Instance, GS.netManagerServer.netManager, null);
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;
            netMessage.writer.Put((DateTime.Now - GS.GetWorld().startTime).Ticks);
            netMessage.writer.Put(ServerSequence);
            netMessage.writer.Put(GS.GetWorld().GetTime());
            netMessage.writer.Put((ulong)GS.GetWorld().WorldInfo.flags.Flags);

            netMessage.Send();

            ServerSequence += 1;
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            long ticks = reader.GetLong();
            TimeSpan timeSent = new TimeSpan(ticks);
            ClientSequence = reader.GetInt();
            float time = reader.GetFloat();
            ulong flags = reader.GetULong();

            //time = (float)timeSent.TotalSeconds;

            //GS.GetWorld()?.SetTime(time);

            if (time < GS.GetClient().Current().time || ClientSequence < GS.GetClient().Current().sequence)
                return;
            GS.GetClient().Current().flags.Flags = (WorldFlags.FlagValues)flags;
            GS.GetClient().NewFrame(ClientSequence, time);

            var expected = GS.GetClient().LastFrameTime + World.SyncTime;
            IMGUINetworkDebug.AddServerFrame(new IMGUINetworkDebug.NetworkDebugFrame 
            {
                frame = ClientSequence,
                actualTime = time,
                expectedTime = expected,
                variance = time - expected,
            });
        }
    }
}
