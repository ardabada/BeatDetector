using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatDetectorTry
{
    public class SubbandBeatDetection
    {
        public int NumSubbands { get; private set; }
        public int NumSamples { get; private set; }

        public SubBand[] SubBands { get; set; }
        private const float a = 0.44f;
        private const float b = 1.56f;

        private const int beatSensivity = 5;
        private const float varianceSensivity = 0.00001f;

        public event EventHandler OnBeat;

        public SubbandBeatDetection(int numSamples, int numSubbands)
        {
            NumSamples = numSamples;
            NumSubbands = numSubbands;

            //samplesLeftChannel = new float[NumSamples];
            //samplesRightChannel = new float[NumSamples];
            SubBands = new SubBand[NumSubbands];

            // Create each subbband
            for (int i = 0; i < SubBands.Length; i++)
            {
                SubBands[i] = new SubBand(i + 1);
            }

        }

        public void OnFFTCalculated(float[] data)
        {
            bool isBeat = false;
            for (int i = 0; i < SubBands.Length; i++)
            {

                int startPoint = 0;
                for (int j = 0; j <= i - 1; j++)
                {
                    startPoint += SubBands[j].FrequencyWidth;
                }
                int endPoint = 0;
                for (int j = 0; j <= i; j++)
                {
                    endPoint += SubBands[j].FrequencyWidth;
                }


                SubBands[i].ComputeInstantEnergy(startPoint, endPoint, data);
                SubBands[i].ComputeAverageEnergy();
                SubBands[i].ComputeInstantVariance();

                isBeat |= SubBands[i].HasBeated();

            }
            if (isBeat)
                OnBeat?.Invoke(this, EventArgs.Empty);
        }

        public class SubBand
        {
            public delegate void OnBeatHandler();
            public event OnBeatHandler OnBeat;

            public float InstantEnergy;
            public float AverageEnergy;
            public float InstantVariance;

            public int FrequencyWidth;
            public float[] HistoryBuffer;

            public int Index;
            public SubBand(int _index)
            {
                Index = _index;
                FrequencyWidth = (int)Math.Round(a * _index + b);
                HistoryBuffer = new float[43];
            }

            public void ComputeInstantEnergy(int _start, int _end, float[] data)
            {
                float result = 0;
                // Start and End means where we are going to do the calculations
                // since each subband works on a certain part of our 1024 samples array
                // Go to reference to know more
                for (int i = _start; i < _end; i++)
                {
                    result += (float)System.Math.Pow(data[i], 2);
                }


                InstantEnergy = result;
            }

            public void ComputeAverageEnergy()
            {
                float result = 0;

                for (int i = 0; i < HistoryBuffer.Length; i++)
                {
                    result += HistoryBuffer[i];
                }

                AverageEnergy = result / HistoryBuffer.Length;

                // Shift the history buffer one position to the right
                // Save it in another array
                float[] shiftedHistoryBuffer = ShiftArray(HistoryBuffer, 1);

                // Make the first position to be our instantEnergy
                shiftedHistoryBuffer[0] = InstantEnergy;

                // Override the elements of the new array on the old one
                OverrideElementsToAnotherArray(shiftedHistoryBuffer, HistoryBuffer);

            }

            public void ComputeInstantVariance()
            {
                float result = 0;

                for (int i = 0; i < HistoryBuffer.Length; i++)
                {
                    result += (float)System.Math.Pow(HistoryBuffer[i] - AverageEnergy, 2);
                }

                InstantVariance = result / HistoryBuffer.Length;
            }

            public bool HasBeated()
            {
                if (InstantEnergy > (beatSensivity * AverageEnergy) && InstantVariance > varianceSensivity)
                {
                    if (OnBeat != null)
                        OnBeat();
                    System.Diagnostics.Debug.WriteLine("Beat " + Index);
                    return true;
                }
                return false;

            }

            #region UTILITY_USE
            private void OverrideElementsToAnotherArray(float[] _from, float[] _to)
            {
                for (int i = 0; i < _from.Length; i++)
                {
                    _to[i] = _from[i];
                }
            }

            private float[] ShiftArray(float[] _array, int amount)
            {

                float[] result = new float[_array.Length];

                for (int i = 0; i < _array.Length - amount; i++)
                {
                    result[i + amount] = _array[i];
                }

                return result;

            }
            #endregion
        }
    }
}
