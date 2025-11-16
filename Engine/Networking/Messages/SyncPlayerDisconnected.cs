using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Networking.Messages
{
    // TODO
    // Sent to all clients when a client disconnects
    // includes player id that disconnected
    // receive should update net players list and remove the now inactive player
    class SyncPlayerDisconnected
    {
    }
}
