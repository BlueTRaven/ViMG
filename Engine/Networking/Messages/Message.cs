using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Networking.Messages
{
    public static class MessageHelper
    {
        public enum MessageType
        {
            SendChunk,
            SendEnt,
            Custom
        }

        public struct MessageHeader 
        {
            MessageType messageType;
        }
    }
}
