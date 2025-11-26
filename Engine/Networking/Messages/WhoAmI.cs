using BepuPhysics.Constraints;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Networking.Messages
{
    public class WhoAmI : Message
    {
        public static WhoAmI Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public WhoAmI()
        {
            Instance = this;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            int whoAmI = addData as int? ?? -1;
            netMessage.writer.Put(whoAmI);
            netMessage.writer.Put(Main.Time - NetworkManager.TIME_TRAVEL_DELAY);
            netMessage.writer.Put(Main.Frame - (int)Math.Floor((double)Main.FIXED_FPS * NetworkManager.TIME_TRAVEL_DELAY));

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            int whoAmI = reader.GetInt();
            GS.netManager.whoAmI = whoAmI;
            Main.Time = reader.GetDouble();
            Main.Frame = reader.GetInt();

            Console.WriteLine("Our player id: {0}\nTime: {1}", whoAmI, Main.Time);
        }
    }
}
