using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDP_EASY
{
    class ClientData
    {
        public short CurrentGame;
        public System.Diagnostics.Stopwatch LastUpdate;

        public ClientData()
        {
            CurrentGame = 0;
            LastUpdate = new System.Diagnostics.Stopwatch();
            LastUpdate.Start();
        }
    }

    
}
