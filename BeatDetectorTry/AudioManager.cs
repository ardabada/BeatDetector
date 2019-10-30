using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel;

namespace BeatDetectorTry
{
    public class AudioManager
    {
        private bool canPlay;
        private bool canPause;
        private bool canStop;
        private bool isPlaying;
        private WaveOut waveOutDevice;
        private readonly int fftDataSize = (int)FFTDataSize.FFT2048;
        private WaveStream activeStream;

        public WaveStream ActiveStream
        {
            get { return activeStream; }
            protected set
            {
                WaveStream oldValue = activeStream;
                activeStream = value;
                //if (oldValue != activeStream)
                //    NotifyPropertyChanged("ActiveStream");
            }
        }
        public double ChannelLength { get; private set; }
        public bool CanPlay { get; private set; }
        public bool CanPause { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool CanStop { get; private set; }

        private WaveChannel32 inputStream;
        private SampleAggregator sampleAggregator;
        SubbandBeatDetection beatDetection;
        public event EventHandler OnBeat;
        TestDetector detector;

        public AudioManager()
        {
            waveformGenerator = new BackgroundWorker();
            waveformGenerator.WorkerSupportsCancellation = true;
            waveformGenerator.DoWork += WaveformGenerator_DoWork;
            waveformGenerator.RunWorkerCompleted += WaveformGenerator_RunWorkerCompleted;
        }

        public void Stop()
        {
            if (waveOutDevice != null)
            {
                waveOutDevice.Stop();
            }
            IsPlaying = false;
            CanStop = false;
            CanPlay = true;
            CanPause = false;
        }

        public void Pause()
        {
            if (IsPlaying && CanPause)
            {
                waveOutDevice.Pause();
                IsPlaying = false;
                CanPlay = true;
                CanPause = false;
            }
        }

        public void Play()
        {
            if (CanPlay)
            {
                waveOutDevice.Play();
                IsPlaying = true;
                CanPause = true;
                CanPlay = false;
                CanStop = true;
            }
        }

        public void OpenFile(string path)
        {
            Stop();

            stopAndCloseStream();

            if (File.Exists(path))
            {
                try
                {
                    waveOutDevice = new WaveOut()
                    {
                        DesiredLatency = 100
                    };
                    ActiveStream = new Mp3FileReader(path);
                    inputStream = new WaveChannel32(ActiveStream);
                    sampleAggregator = new SampleAggregator(fftDataSize);
                    inputStream.Sample += inputStream_Sample;
                    waveOutDevice.Init(inputStream);
                    ChannelLength = inputStream.TotalTime.TotalSeconds;
                    beatDetection = new SubbandBeatDetection(1024, 64);
                    beatDetection.OnBeat += (s, e) => OnBeat?.Invoke(this, EventArgs.Empty);
                    detector = new TestDetector();
                    detector.onBeat += (s, e) => OnBeat?.Invoke(this, EventArgs.Empty);
                    detector.Start();
                    //GenerateWaveformData(path);
                    CanPlay = true;
                }
                catch
                {
                    ActiveStream = null;
                    CanPlay = false;
                }
            }
        }
        private void stopAndCloseStream()
        {
            if (waveOutDevice != null)
            {
                waveOutDevice.Stop();
            }
            if (activeStream != null)
            {
                inputStream.Close();
                inputStream = null;
                ActiveStream.Close();
                ActiveStream = null;
            }
            if (waveOutDevice != null)
            {
                waveOutDevice.Dispose();
                waveOutDevice = null;
            }
        }
        private void inputStream_Sample(object sender, SampleEventArgs e)
        {
            sampleAggregator.Add(e.Left, e.Right);
        }

        public bool GetFFTData(float[] fftDataBuffer)
        {
            sampleAggregator.GetFFTResults(fftDataBuffer);
            //beatDetection.OnFFTCalculated(fftDataBuffer);
            detector.Update(fftDataBuffer.Take(1024).ToArray());
            return isPlaying;
        }

        public int GetFFTFrequencyIndex(int frequency)
        {
            double maxFrequency;
            if (ActiveStream != null)
                maxFrequency = ActiveStream.WaveFormat.SampleRate / 2.0d;
            else
                maxFrequency = 22050; // Assume a default 44.1 kHz sample rate.
            return (int)((frequency / maxFrequency) * (fftDataSize / 2));
        }



        public int waveformCompressedPointCount = 2000;
        BackgroundWorker waveformGenerator;
        SampleAggregator waveformAggregator;
        public event EventHandler OnWaveformCalculated;
        public WaveformGenerationResult Waveform { get; private set; }
        public void GenerateWaveformData(string path)
        {
            if (waveformGenerator.IsBusy)
                return;
            waveformGenerator.RunWorkerAsync(new WaveformGenerationParams(waveformCompressedPointCount, path));
        }
        
        private void WaveformGenerator_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Waveform = e.Result as WaveformGenerationResult;
            OnWaveformCalculated?.Invoke(this, EventArgs.Empty);
        }

        private void WaveformGenerator_DoWork(object sender, DoWorkEventArgs e)
        {
            WaveformGenerationParams waveformParams = e.Argument as WaveformGenerationParams;
            Mp3FileReader waveformMp3Stream = new Mp3FileReader(waveformParams.Path);
            WaveChannel32 waveformInputStream = new WaveChannel32(waveformMp3Stream);
            waveformInputStream.Sample += waveStream_Sample;

            int frameLength = fftDataSize;
            int frameCount = (int)((double)waveformInputStream.Length / (double)frameLength);
            int waveformLength = frameCount * 2;
            byte[] readBuffer = new byte[frameLength];
            waveformAggregator = new SampleAggregator(frameLength);

            float maxLeftPointLevel = float.MinValue;
            float maxRightPointLevel = float.MinValue;
            int currentPointIndex = 0;
            float[] waveformCompressedPoints = new float[waveformParams.Points];
            List<float> waveformData = new List<float>();
            List<int> waveMaxPointIndexes = new List<int>();

            for (int i = 1; i <= waveformParams.Points; i++)
            {
                waveMaxPointIndexes.Add((int)Math.Round(waveformLength * ((double)i / (double)waveformParams.Points), 0));
            }
            int readCount = 0;
            while (currentPointIndex * 2 < waveformParams.Points)
            {
                waveformInputStream.Read(readBuffer, 0, readBuffer.Length);

                waveformData.Add(waveformAggregator.LeftMaxVolume);
                waveformData.Add(waveformAggregator.RightMaxVolume);

                if (waveformAggregator.LeftMaxVolume > maxLeftPointLevel)
                    maxLeftPointLevel = waveformAggregator.LeftMaxVolume;
                if (waveformAggregator.RightMaxVolume > maxRightPointLevel)
                    maxRightPointLevel = waveformAggregator.RightMaxVolume;

                if (readCount > waveMaxPointIndexes[currentPointIndex])
                {
                    waveformCompressedPoints[(currentPointIndex * 2)] = maxLeftPointLevel;
                    waveformCompressedPoints[(currentPointIndex * 2) + 1] = maxRightPointLevel;
                    maxLeftPointLevel = float.MinValue;
                    maxRightPointLevel = float.MinValue;
                    currentPointIndex++;
                }

                if (waveformGenerator.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                readCount++;
            }

            float[] finalClonedData = (float[])waveformCompressedPoints.Clone();
            e.Result = new WaveformGenerationResult(waveformData.ToArray(), finalClonedData);
            //App.Current.Dispatcher.Invoke(new Action(() =>
            //{
            //    fullLevelData = waveformData.ToArray();
            //    WaveformData = finalClonedData;
            //}));
            waveformInputStream.Close();
            waveformInputStream.Dispose();
            waveformInputStream = null;
            waveformMp3Stream.Close();
            waveformMp3Stream.Dispose();
            waveformMp3Stream = null;
        }

        private void waveStream_Sample(object sender, SampleEventArgs e)
        {
            waveformAggregator.Add(e.Left, e.Right);
        }
    }
}
