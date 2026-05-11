using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccidentReport
{
    public class VictimData
    {
        public int Drivers { get; set; }
        public int Passengers { get; set; }
        public int Pedestrians { get; set; }
        public int Cyclists { get; set; }

        public int Total => Drivers + Passengers + Pedestrians + Cyclists;

    }
}
