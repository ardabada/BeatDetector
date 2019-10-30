using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BeatDetectorHost
{
    public partial class MasterPeakDrawer : UserControl
    {
        public delegate void ManualValueEventHandler(double value);
        public event ManualValueEventHandler OnManualPeakChanged;

        public MasterPeakDrawer()
        {
            InitializeComponent();
        }

        public void SetPeak(double peak)
        {
            peakGrid.Width = peak * innerGrid.ActualWidth /*/ 100*/;
        }
        public void SetAverage(double avg)
        {
            double left = avg * innerGrid.ActualWidth /*/ 100*/ - averagePeakGrid.Width / 2;
            averagePeakGrid.Margin = new Thickness(left, 0, 0, 0);
        }
        public void SetSilenceArea(double silence)
        {
            silenceArea.Width = silence * innerGrid.ActualWidth/* / 100*/;
        }
        public void SetManualSlider(double manual, bool triggerEvent = true)
        {
            if (!IsManualMode)
                return;
            if (manual < 0)
                manual = 0;
            if (manual > 1)
                manual = 1;
            double left = manual * innerGrid.ActualWidth /*/ 100*/ - peakManualGreed.Width / 2;
            if (left < 0)
                left = 0;
            if (left > innerGrid.ActualWidth - peakManualGreed.Width)
                left = innerGrid.ActualWidth - peakManualGreed.Width;
            peakManualGreed.Margin = new Thickness(left, 0, 0, 0);
            if (triggerEvent)
                OnManualPeakChanged?.Invoke(manual);
        }

        private bool isManualMode = false;
        public bool IsManualMode
        {
            get
            {
                return isManualMode;
            }
            set
            {
                isManualMode = value;
                peakManualGreed.Visibility = value ? Visibility.Visible : Visibility.Hidden;
                averagePeakGrid.Visibility = value ? Visibility.Hidden : Visibility.Visible;
            }
        }


        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsManualMode)
                return;
            setManualPoint(e);
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsManualMode)
                return;
            if (e.LeftButton == MouseButtonState.Pressed)
                setManualPoint(e);
        }

        void setManualPoint(MouseEventArgs e)
        {
            var value = e.GetPosition(this).X;
            //width = 100%
            //value - x
            SetManualSlider(value /** 100*/ / Width);
        }
    }
}
