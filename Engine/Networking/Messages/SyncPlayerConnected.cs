using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Networking.Messages
{
    public class SyncPlayerConnected : Message
    {
        public static SyncPlayerConnected Instance { get; private set; }

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;
        public SyncPlayerConnected()
        {
            Instance = this;
        }

        public override void SendMessage(NetDataWriter writer, object? addData) 
        {
            base.SendMessage(writer, addData);

            writer.Put(addData as int? ?? -1);
            writer.Put(GS.netManager.netPlayers.Count);
            foreach (NetworkManager.NetPlayer player in GS.netManager.netPlayers)
            {
                writer.Put(player);
            }
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            GS.netManager.netPlayers.Clear();
            int whoAmI = reader.GetInt();
            if (whoAmI != -1)
                GS.netManager.whoAmI = whoAmI;
            int numPlayers = reader.GetInt();
            for (int i = 0; i < numPlayers; i++)
                GS.netManager.netPlayers.Add(reader.Get<NetworkManager.NetPlayer>());
            Console.WriteLine("Our player id: {0}", whoAmI);
            GS.GetWorld().localPlayerIndex = whoAmI;
        }
    }
}
