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
    public class WhoAmIRequest : Message
    {
        private static Engine.Logger Logger = Engine.Logger.InitLogger("WhoAmIRequest", true, Engine.Logger.LogLevel.Info);

        public static WhoAmIRequest Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Client;

        public WhoAmIRequest()
        {
            Instance = this;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);
            netMessage.deliveryMethod = DeliveryMethod.ReliableOrdered;

            if (GS.localPlayerName != null)
                netMessage.writer.Put(GS.localPlayerName);
            else netMessage.writer.Put("");

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            string playerName = reader.GetString();

            if (playerName == "" || GS.netManagerServer.GetNetPlayerByName(playerName).playerId != -1)
            {
                Logger.Log(Engine.Logger.LogLevel.Error, "Invalid player name ({0}) recieved from {1}", playerName, peer.ToString());
                peer.Disconnect();
                return;
            }

            GlobalState.GameStateManager.TheIsland.netManagerServer.NewPlayer(peer, playerName);
        }
    }

    public class WhoAmI : Message
    {
        private static Engine.Logger Logger = Engine.Logger.InitLogger("WhoAmI", true, Engine.Logger.LogLevel.Info);

        public static WhoAmI Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public WhoAmI()
        {
            Instance = this;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            netMessage.deliveryMethod = DeliveryMethod.ReliableOrdered;

            int whoAmI = addData as int? ?? -1;
            netMessage.writer.Put(whoAmI);
            netMessage.writer.Put(GlobalState.Time - NetworkManager.TIME_TRAVEL_DELAY);
            netMessage.writer.Put(Main.Frame - (int)Math.Floor((double)Main.FIXED_FPS * NetworkManager.TIME_TRAVEL_DELAY));

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            double oldTime = GlobalState.Time;
            int whoAmI = reader.GetInt();
            GlobalState.Time = reader.GetDouble();
            Main.Frame = reader.GetInt();

            if (whoAmI != -1)
            {
                GS.netManagerClient.whoAmI = whoAmI;
                Logger.Log(Engine.Logger.LogLevel.Info, "Our player id: {0}\nTime: {1}", whoAmI, GlobalState.Time);
            } else
            {
                //Console.WriteLine("Fix time: {0:0.02}", GlobalState.Time - oldTime);
            }
        }
    }
}
