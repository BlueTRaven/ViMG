using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            int whoAmI = reader.GetInt();
            GS.netManager.whoAmI = whoAmI;

            Console.WriteLine("Our player id: {0}", whoAmI);
        }
    }
}
