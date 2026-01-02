using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.GameStates;

namespace Engine.Networking.Messages
{
    public abstract class Message : IRegisterable
    {
        public string Identifier => GetType().FullName;
        public int Id;

        public abstract NetworkManager.NetworkSide SendableFrom { get; }
        // Passthrough messages are sent from a client to a server and are immediately re-distributed from the server to all other clients (exluding the one who sent it)
        public bool Passthrough = false; 

        protected GameStateTheIsland GS => Main.gameStateManager.TheIsland;

        public virtual void SendMessage(NetworkMessage netMessage, object? addData)
        {

        }

        public virtual void SendMessage<T>(NetworkMessage netMessage, T addData) { }

        public virtual void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            
        }
    }
}
