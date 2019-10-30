using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BeatDetectorHost
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();

            Loaded += (s, e) =>
            {
                getDevices();
                //updateTargetDevice();
                loaded = true;
                SilenceLevel = 0.1;
                updateTargetDevice();
            };
            //Loaded += (s, e) =>
            //{
            //    new OverlayWindow().Show();
            //};
            //this.ToTransparentWindow();

            //Deactivated += (s, e) =>
            //{
            //    Activate();
            //    Focus();
            //};
        }

        OverlayWindow overlay;

        DispatcherTimer timer;
        MMDeviceEnumerator audioDeviceEnumerator;
        MMDevice targetDevice;
        bool loaded = false, canTriggerManulPeak = true, canTriggerManulPeakChange = true;
        int averageCount = 5;
        public int AverageCount
        {
            get { return averageCount; }
            set
            {
                averageCount = value;
                if (averageCount < 1)
                    averageCount = 1;
                else if (averageCount > 100)
                    averageCount = 100;
            }
        }

        MMDeviceCollection getOutputDevices()
        {
            return audioDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        }
        MMDeviceCollection getInputDevices()
        {
            return audioDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
        }

        private double _silenceLevel = 0.1;
        public double SilenceLevel
        {
            get { return _silenceLevel; }
            set
            {
                _silenceLevel = value;
                if (loaded)
                {
                    silenceLevelLbl.Text = (value * 100).ToString("N0") + "%";
                    masterPeak.SetSilenceArea(value);
                }
            }
        }
        private bool _manualPeakLevel = false;
        public bool IsManualPeakLevel
        {
            get { return _manualPeakLevel; }
            set
            {
                canTriggerManulPeak = false;
                _manualPeakLevel = value;
                masterPeak.IsManualMode = value;
                manualPeekLevelCheckbox.IsChecked = value;
                canTriggerManulPeak = true;
            }
        }

        void getDevices()
        {
            audioDeviceEnumerator = new MMDeviceEnumerator();
            var inputDevices = getInputDevices();
            var outputDevices = getOutputDevices();
            foreach (var input in inputDevices)
                inputDevicesCombobox.Items.Add(input.FriendlyName);
            foreach (var output in outputDevices)
                outputDevicesCombobox.Items.Add(output.FriendlyName);
            if (inputDevices.Any())
                inputDevicesCombobox.SelectedIndex = 0;
            if (outputDevices.Any())
                outputDevicesCombobox.SelectedIndex = 0;
        }

        void updateTargetDevice()
        {
            if (outputDeviceRadioButton.IsChecked.HasValue && outputDeviceRadioButton.IsChecked.Value)
            {
                if (outputDevicesCombobox.SelectedIndex > -1)
                {
                    var outputDevices = getOutputDevices();
                    if (outputDevices.Count > outputDevicesCombobox.SelectedIndex)
                        targetDevice = outputDevices[outputDevicesCombobox.SelectedIndex];
                }
            }
            else
            {
                if (inputDevicesCombobox.SelectedIndex > -1)
                {
                    var inputDevices = getInputDevices();
                    if (inputDevices.Count > inputDevicesCombobox.SelectedIndex)
                        targetDevice = inputDevices[inputDevicesCombobox.SelectedIndex];
                }
            }
        }

        List<double> peakValues = new List<double>();
        double peakAvg = -1;
        double lastPeakValue = -1;
        bool canToggle = false;
        double manualPeak = 0;
        public double ManualPeakLevel
        {
            get { return manualPeak; }
            set
            {
                canTriggerManulPeakChange = false;
                manualPeak = value;
                if (manualPeak < 0)
                    manualPeak = 0;
                else if (manualPeak > 1)
                    manualPeak = 1;
                masterPeak.SetManualSlider(manualPeak, false);
                canTriggerManulPeakChange = true;
            }
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (targetDevice == null)
                return;
            var peakValue = targetDevice.AudioMeterInformation.MasterPeakValue;
            peakValues.Add(peakValue);
            if (peakValues.Count > 5)
            {
                peakAvg = peakValues.Average();
                peakValues.Clear();
            }
            var limit = IsManualPeakLevel ? manualPeak : peakAvg;
            masterPeak.SetAverage(peakAvg);
            masterPeak.SetPeak(peakValue);
            canToggle = true;
            //if (peakValue < limit)
            //    canToggle = true;
            if (peakValue < SilenceLevel)
                canToggle = false;
            if (canToggle && peakValue > limit)
            {
                if (overlay != null)
                {
                    overlay.Toggle();
                    canToggle = false;
                }
            }
            lastPeakValue = peakValue;
        }

        private void devicesCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
                updateTargetDevice();
        }

        private void deviceRadioButton_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (loaded)
                updateTargetDevice();
        }

        private void silenceLevelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SilenceLevel = e.NewValue;
        }

        private void manualPeekLevelCheckbox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (canTriggerManulPeak)
                IsManualPeakLevel = manualPeekLevelCheckbox.IsChecked.HasValue && manualPeekLevelCheckbox.IsChecked.Value;
        }

        private void debugCheckbox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            setInfoShowing();
        }

        private void masterPeak_OnManualPeakChanged(double value)
        {
            if (canTriggerManulPeakChange)
                ManualPeakLevel = value;
        }

        private void windowBtn_Click(object sender, RoutedEventArgs e)
        {
            launchWindow(false);
        }
        private void overlayBtn_Click(object sender, RoutedEventArgs e)
        {
            launchWindow(true);
        }

        void forceOverlayClose()
        {
            if (overlay.IsOpen())
                overlay.ForceClose();
        }

        void setInfoShowing()
        {
            if (overlay != null)
                overlay.ShowInfo = debugCheckbox.IsChecked.HasValue && debugCheckbox.IsChecked.Value;
        }

        void launchWindow(bool asOverlay)
        {
            forceOverlayClose();

            overlay = new OverlayWindow();
            subscibeEvents();
            setInfoShowing();
            if (asOverlay)
                overlay.SetOverlayStyle();
            else overlay.SetWindowStyle();
            overlay.Show();
        }

        void subscibeEvents()
        {
            if (overlay != null)
            {
                overlay.OnTimerChangeRequested += onTimerChangeRequested;
                overlay.OnAverageCountersRequested += onAverageCountersRequested;
                overlay.ToggleManualPeak += Overlay_ToggleManualPeak;
                overlay.OnManualPeak += Overlay_OnManualPeak;
            }
        }

        private void Overlay_ToggleManualPeak(object sender, EventArgs e)
        {
            IsManualPeakLevel = !IsManualPeakLevel;
            if (overlay != null)
                overlay.SetInfo("Manual peak level " + (IsManualPeakLevel ? "on" : "off"));
        }

        private void Overlay_OnManualPeak(double threshold)
        {
            ManualPeakLevel += threshold;
            if (overlay != null && IsManualPeakLevel)
                overlay.SetInfo("Manual peak level updated: " + (ManualPeakLevel * 100).ToString("N0"));
        }

        //void unsubscibeTimerEvents()
        //{
        //    if (overlay != null)
        //        overlay.OnTimerChangeRequested -= onTimerChangeRequested;
        //}

        void onTimerChangeRequested(double threshold)
        {
            double interval = timer.Interval.TotalMilliseconds + threshold;
            if (interval < 0.5)
                interval = 0.5;
            else if (interval > 50)
                interval = 50;
            timer.Interval = TimeSpan.FromMilliseconds(interval);
            if (overlay != null)
                overlay.SetInfo("Timer interval updated: " + interval.ToString("N2"));
        }
        void onAverageCountersRequested(int threshold)
        {
            AverageCount += threshold;
            if (overlay != null)
                overlay.SetInfo("Average counter updated: " + AverageCount);
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            forceOverlayClose();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            forceOverlayClose();
            timer.Stop();

        }
    }
}
