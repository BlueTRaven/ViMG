using Engine.Networking.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModGameBase.Networking
{
    public class MessageRegistryViMG : MessageRegistry
    {
        protected override void DoRegistration()
        {
            Register(new SyncWeather());
        }
    }
}
