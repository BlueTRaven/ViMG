using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Networking
{
    public interface ISyncBasicState
    {
        void Get(out BasicState state);

        void Set(ref readonly BasicState state);
    }
}
