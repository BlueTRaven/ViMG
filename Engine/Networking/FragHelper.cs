using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Networking
{
    // Helps fragment a message
    // Splits into fragmentable regions, fitting as many regions as it can into one packet when calling Send.
    // Writes the number of regions sent (short) after startPosition and before the regions themselves.
    public struct FragHelper
    {
        private NetworkMessage netMessage;
        private NetDataWriter writer;
        private int startPosition;
        private int maxSize;

        private List<NetDataWriter> subWriters = new List<NetDataWriter>();
        private NetDataWriter currentWriter;

        public FragHelper(NetworkMessage netMessage)
        {
            this.netMessage = netMessage;
            this.writer = netMessage.writer;
            startPosition = writer.Length;
            maxSize = netMessage.peer.GetMaxSinglePacketSize(DeliveryMethod.Unreliable) - sizeof(ushort);
        }

        public NetDataWriter StartFragmentable()
        {
            currentWriter = new();
            return currentWriter;
        }

        public void EndFragmentable()
        {
            Debug.Assert(startPosition + currentWriter.Length < maxSize);
            subWriters.Add(currentWriter);
        }

        public void Send()
        {
            Debug.Assert(netMessage.writer.Length == startPosition);

            netMessage.writer.Put((ushort)0);

            int numSend = 0;
            foreach (var writer in subWriters)
            {
                if (netMessage.writer.Length + writer.Length > maxSize)
                {
                    var end = netMessage.writer.Length;
                    netMessage.writer.SetPosition(startPosition);
                    netMessage.writer.Put((ushort)numSend);
                    netMessage.writer.SetPosition(end);

                    netMessage.Send();
                    netMessage.writer.SetPosition(startPosition + sizeof(ushort));
                    numSend = 1;
                    netMessage.writer.Put(writer.AsReadOnlySpan());
                } else
                {
                    netMessage.writer.Put(writer.AsReadOnlySpan());
                    numSend += 1;
                }
            }

            if (numSend > 0) 
            {
                var end = netMessage.writer.Length;
                netMessage.writer.SetPosition(startPosition);
                netMessage.writer.Put((ushort)numSend);
                netMessage.writer.SetPosition(end);

                netMessage.Send();
                netMessage.writer.SetPosition(startPosition);
            }
        }

        public void Clear(NetworkMessage netMessage)
        {
            currentWriter = null;
            subWriters.Clear();
            
            this.netMessage = netMessage;
            this.writer = netMessage.writer;
            startPosition = writer.Length;
            maxSize = netMessage.peer.GetMaxSinglePacketSize(DeliveryMethod.Unreliable);
        }
    }
}
