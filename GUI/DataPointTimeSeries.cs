using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBCI_GUI
{
    internal class DataPointTimeSeries
    {
        public double time;
        public double voltage;
        public DataPointTimeSeries(double time,double voltage) {
            this.time = time;
            this.voltage = voltage;
        }
    }
}
