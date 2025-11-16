using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Networking.Messages
{
    public class ClientSendInputs : Message
    {
        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Client;

        //public override void SendMessage(NetDataWriter writer, object? addData)
        //{
        //    base.SendMessage(writer, addData);
        //}
    }
}
