using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using NAudio.CoreAudioApi;

namespace BeatDetectorTry
{
    public partial class MainWindow : Window
    {
        bool flag = true;
        const int barsCount = 16;
        Slider[] sliders = new Slider[barsCount];
        Slider averageSlider, averagePeakSlider;
        private float[] channelData = new float[2048];

        private int maximumFrequencyIndex = 2047;
        private int minimumFrequencyIndex;
        public int MaximumFrequency = 20000;
        public int MinimumFrequency = 20;
        public int PeakFallDelay = 10;
        public BarHeightScalingStyles BarHeightScaling = BarHeightScalingStyles.Sqrt;
        public bool IsFrequencyScaleLinear = false;
        public bool AveragePeaks = true;
        private int[] barIndexMax;
        private int[] barLogScaleIndexMax;
        private const int scaleFactorLinear = 9;
        private const int scaleFactorSqr = 2;
        private const double minDBValue = -90;
        private const double maxDBValue = 0;
        private const double dbScale = (maxDBValue - minDBValue);
        private const int defaultUpdateInterval = 25;
        private double[] channelPeakData;

        List<double> peaksData = new List<double>();

        DateTime lastBeat = DateTime.MinValue;
        TimeSpan seizureTime = TimeSpan.FromMilliseconds(100);
        

        AudioManager manager;
        DispatcherTimer timer;
        string file = "song.mp3";
        BeatTester window;

        public MainWindow()
        {
            InitializeComponent();

            window = new BeatTester();
            window.Show();
            //Close();

            initBars();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(5);
            timer.Tick += Timer_Tick;
            timer.Start();

            //manager = new AudioManager();
            ////manager.OnBeat += (s, e) => Toggle();
            //manager.OnWaveformCalculated += Manager_OnWaveformCalculated;
            //manager.GenerateWaveformData(file);
        }

        List<Tuple<TimeSpan, TimeSpan>> timings = new List<Tuple<TimeSpan, TimeSpan>>();
        private void Manager_OnWaveformCalculated(object sender, EventArgs e)
        {
            float[] waveform = new float[manager.Waveform.WaveformData.Length / 2];
            for (int i = 0, j = 0; i < manager.Waveform.WaveformData.Length; i+=2, j++)
            {
                waveform[j] = manager.Waveform.WaveformData[i];
            }

            ChorusFinder finder = new ChorusFinder(waveform);
            var data = finder.FindChorusParts().OrderByDescending(x => x.Item3).ToList();

            //manager.ActiveStream.TotalTime - 1000
            //manager.ActiveStream.CurrentTime - x

            manager.OpenFile(file);
            //manager.Play();

            timer.Start();

            int points = manager.waveformCompressedPointCount / 2;
            double totalMilliseconds = manager.ActiveStream.TotalTime.TotalMilliseconds;
            foreach (var i in data)
            {
                int start = i.Item1,
                    next = i.Item2,
                    length = i.Item3;

                //total ms = points
                //x ms = start

                double startMs = start * totalMilliseconds / points;
                double nextMs = next * totalMilliseconds / points;
                double startMs_End = (start+length) * totalMilliseconds / points;
                double nextMs_End = (next+length) * totalMilliseconds / points;

                timings.Add(new Tuple<TimeSpan, TimeSpan>(TimeSpan.FromMilliseconds(startMs), TimeSpan.FromMilliseconds(startMs_End)));
                timings.Add(new Tuple<TimeSpan, TimeSpan>(TimeSpan.FromMilliseconds(nextMs), TimeSpan.FromMilliseconds(nextMs_End)));
            }
        }



        public void Toggle(bool force = false)
        {
            if (force || DateTime.Now - lastBeat >= seizureTime)
            {
                flag = !flag;
                Color a = Colors.Red;
                Color b = Colors.Blue;
                if (flag)
                {
                    a = Colors.Blue;
                    b = Colors.Red;
                }
                grid1.Background = new SolidColorBrush(a);
                grid2.Background = new SolidColorBrush(b);

                lastBeat = DateTime.Now;
            }
        }

        void initBars()
        {
            averagePeakSlider = new Slider();
            averagePeakSlider.Orientation = Orientation.Vertical;
            averagePeakSlider.Height = 300;
            averagePeakSlider.Maximum = 100;
            averagePeakSlider.Width = 25;
            averagePeakSlider.Background = new SolidColorBrush(Colors.Blue);
            bars.Children.Add(averagePeakSlider);

            averageSlider = new Slider();
            averageSlider.Orientation = Orientation.Vertical;
            averageSlider.Height = 300;
            averageSlider.Maximum = 100;
            averageSlider.Width = 25;
            averageSlider.Background = new SolidColorBrush(Colors.Red);
            bars.Children.Add(averageSlider);

            for (int i = 0; i < barsCount; i++)
            {
                Slider s = new Slider();
                s.Orientation = Orientation.Vertical;
                s.Height = 300;
                s.Maximum = 300;
                s.Width = 25;
                bars.Children.Add(s);
                sliders[i] = s;
            }
        }

        List<double> peakValues = new List<double>();
        double peakAvg = -1;
        double lastPeakValue = -1;
        bool canToggle = false;
        private void Timer_Tick(object sender, EventArgs e)
        {
            var device = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var peakValue = device.AudioMeterInformation.MasterPeakValue;
            peakValues.Add(peakValue);
            if (peakValues.Count > 10)
            {
                peakAvg = peakValues.Average();
                peakValues.Clear();
            }
            masterPeakLimitSlider.Value = peakAvg * 100;
            masterPeakSlider.Value = peakValue * 100;
            if (peakValue < peakAvg)
                canToggle = true;
            if (peakValue < 0.1)
                canToggle = false;
            if (canToggle && peakValue > peakAvg)
            {
                window.Toggle();
                canToggle = false;
            }
            lastPeakValue = peakValue;
            //updateSpectrum();

            //checkChorus();
        }

        void checkChorus()
        {
            TimeSpan actualTime = manager.ActiveStream.CurrentTime;
            bool chorus = timings.Where(x => actualTime>= x.Item1 && actualTime <= x.Item2).Any();
            Color back = chorus ? Colors.Black : Colors.White;
            this.Background = new SolidColorBrush(back);
        }

        void updateSpectrum()
        {
            manager.GetFFTData(channelData);
                //return;
            updateSpectrumBars();
        }

        int prevPeaksCount = 0;
        void updateSpectrumBars()
        {
            maximumFrequencyIndex = Math.Min(manager.GetFFTFrequencyIndex(MaximumFrequency) + 1, 2047);
            minimumFrequencyIndex = Math.Min(manager.GetFFTFrequencyIndex(MinimumFrequency), 2047);
            int indexCount = maximumFrequencyIndex - minimumFrequencyIndex;
            int linearIndexBucketSize = (int)Math.Round((double)indexCount / (double)barsCount, 0);
            List<int> maxIndexList = new List<int>();
            List<int> maxLogScaleIndexList = new List<int>();
            double maxLog = Math.Log(barsCount, barsCount);
            for (int i = 1; i < barsCount; i++)
            {
                maxIndexList.Add(minimumFrequencyIndex + (i * linearIndexBucketSize));
                int logIndex = (int)((maxLog - Math.Log((barsCount + 1) - i, (barsCount + 1))) * indexCount) + minimumFrequencyIndex;
                maxLogScaleIndexList.Add(logIndex);
            }
            maxIndexList.Add(maximumFrequencyIndex);
            maxLogScaleIndexList.Add(maximumFrequencyIndex);
            barIndexMax = maxIndexList.ToArray();
            barLogScaleIndexMax = maxLogScaleIndexList.ToArray();
            channelPeakData = new double[barsCount];


            double barHeight = 0f;
            double fftBucketHeight = 0f;
            double height = 300;
            double barHeightScale = height;
            int barIndex = 0;
            double lastPeakHeight = 0f;
            double peakYPos = 0f;

            double[] values = new double[barsCount];

            for (int i = minimumFrequencyIndex; i <= maximumFrequencyIndex; i++)
            {
                // If we're paused, keep drawing, but set the current height to 0 so the peaks fall.
                if (!manager.IsPlaying)
                {
                    barHeight = 0f;
                }
                else // Draw the maximum value for the bar's band
                {
                    switch (BarHeightScaling)
                    {
                        case BarHeightScalingStyles.Decibel:
                            double dbValue = 20 * Math.Log10((double)channelData[i]);
                            fftBucketHeight = ((dbValue - minDBValue) / dbScale) * barHeightScale;
                            break;
                        case BarHeightScalingStyles.Linear:
                            fftBucketHeight = (channelData[i] * scaleFactorLinear) * barHeightScale;
                            break;
                        case BarHeightScalingStyles.Sqrt:
                            fftBucketHeight = (((Math.Sqrt((double)channelData[i])) * scaleFactorSqr) * barHeightScale);
                            break;
                    }

                    if (barHeight < fftBucketHeight)
                        barHeight = fftBucketHeight;
                    if (barHeight < 0f)
                        barHeight = 0f;
                }

                // If this is the last FFT bucket in the bar's group, draw the bar.
                int currentIndexMax = IsFrequencyScaleLinear ? barIndexMax[barIndex] : barLogScaleIndexMax[barIndex];
                if (i == currentIndexMax)
                {
                    // Peaks can't surpass the height of the control.
                    if (barHeight > height)
                        barHeight = height;

                    if (AveragePeaks && barIndex > 0)
                        barHeight = (lastPeakHeight + barHeight) / 2;


                    sliders[barIndex].Value = barHeight;
                    values[barIndex] = barHeight * 100 / height;
                    //peakYPos = barHeight;

                    //if (channelPeakData[barIndex] < peakYPos)
                    //    channelPeakData[barIndex] = (float)peakYPos;
                    //else
                    //    channelPeakData[barIndex] = (float)(peakYPos + (PeakFallDelay * channelPeakData[barIndex])) / ((float)(PeakFallDelay + 1));

                    //double xCoord = BarSpacing + (barWidth * barIndex) + (BarSpacing * barIndex) + 1;

                    //barShapes[barIndex].Margin = new Thickness(xCoord, (height - 1) - barHeight, 0, 0);
                    //barShapes[barIndex].Height = barHeight;
                    //peakShapes[barIndex].Margin = new Thickness(xCoord, (height - 1) - channelPeakData[barIndex] - peakDotHeight, 0, 0);
                    //peakShapes[barIndex].Height = peakDotHeight;

                    //if (channelPeakData[barIndex] > 0.05)
                    //    allZero = false;

                    lastPeakHeight = barHeight;
                    barHeight = 0f;
                    barIndex++;
                }
            }
            
            if (values[0] > 60 && values[1] > 40)
            {
                Toggle();
            }
            //averageSlider.Value = values.Average();
            //values = values.OrderByDescending(x => x).ToArray();
            //float limit = 30.0f;
            //List<double> peaks = new List<double>();
            //for (int i = 0; i < values.Length-1; i++)
            //{
            //    peaks.Add(values[i]);
            //    if (Math.Abs(values[i] - values[i + 1]) < limit)
            //        continue;
            //    else break;
            //}
            //averagePeakSlider.Value = peaks.Average();
            //int peaksCount = peaks.Count;
            //if (peaks.Count < prevPeaksCount)
            //    Toggle(true);
            //else if (peaks.Count > 1)
            //{
            //    var max1 = peaks.Max();
            //    peaks.Remove(max1);
            //    var max2 = peaks.Max();
            //    if (max1 - max2 > 10)
            //        Toggle();
            //}
            //prevPeaksCount = peaksCount;

            //Title = string.Join(" | ", peaks.ToArray()); // peaks.Join(" | ");
            //if (peaks.Any())
            //{
            //    double max = peaks.Max();
            //    if (peaksData.Any())
            //    {
            //        if (max - peaksData.Skip(peaksData.Count - 20).Average() > 10)
            //            Toggle();
            //    }
            //    peaksData.Add(max);
                

            //    //double avg = peaks.Max();
            //    //if (peaksData.Any())
            //    //{
            //    //    var max = peaksData.Max();
            //    //    if (avg > max)
            //    //        Toggle();
            //    //}
            //    //peaksData.Enqueue(avg);
            //    ////if (avg - peaksData.First() > limit)
            //    ////    Toggle();
            //    //if (peaksData.Count > 100)
            //    //    peaksData.Dequeue();
            //    masterPeakLimitSlider.Value = max;
            //}
        }

        //bool analyzePeaks()
        //{
        //    //for (int i = peaksData.Count - 1; i >= 0; i--)
        //    //{

        //    //}
        //}
    }
}
