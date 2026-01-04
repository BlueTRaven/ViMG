using LiteNetLib;
using ModGameBase.Client.WorldLogics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.WorldLogics;

namespace Engine.Networking.Messages
{
    public class SyncWeather : Message
    {
        public static SyncWeather Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public SyncWeather()
        {
            Instance = this;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            if (GS.GetWorld().Logic is WorldLogicIsland logicIsland)
            {
                logicIsland.WeatherManager.Serialize(netMessage.writer);
                netMessage.writer.Put(logicIsland.WeatherChangeTimer);

                netMessage.Send();
            }
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            if (GS.GetClient()?.WorldLogic is ClientWorldLogicIsland logicIsland)
            {
                logicIsland.WeatherManager?.Deserialize(reader);
                reader.GetFloat();
                //logicIsland.WeatherChangeTimer = reader.GetFloat();
            }
        }
    }
}
