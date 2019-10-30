using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatDetectorTry
{

    public class WaveformGenerationParams
    {
        public WaveformGenerationParams(int points, string path)
        {
            Points = points;
            Path = path;
        }

        public int Points { get; protected set; }
        public string Path { get; protected set; }
    }

    public class WaveformGenerationResult
    {
        public WaveformGenerationResult(float[] fullLevelData, float[] waveformData)
        {
            FullLevelData = fullLevelData;
            WaveformData = waveformData;
        }

        public float[] FullLevelData { get; private set; }
        public float[] WaveformData { get; private set; }
    }
}
