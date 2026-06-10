using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Networking
{
    public interface ISyncedEntity
    {
        void GetSyncedEntity(out SyncedEntity state);
    }
}
