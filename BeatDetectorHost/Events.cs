using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatDetectorHost
{
    public delegate void OnTimerRequestedEventHandler(double threshold);
    public delegate void OnAverageCountersRequestedEventHandler(int threshold);
    public delegate void OnManualPeakEventHandler(double threshold);

    //public class TimerIncreaseEventArgs : EventArgs
    //{
    //    public TimerIncreaseEventArgs(double threshold)
    //    {
    //        Threshold = threshold;
    //    }

    //    public double Threshold { get; private set; }
    //}
}
