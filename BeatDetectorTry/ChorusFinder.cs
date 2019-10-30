using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatDetectorTry
{
    public class ChorusFinder
    {
        private float[] waveformData;

        public ChorusFinder(float[] waveform)
        {
            waveformData = waveform;
        }

        public List<Tuple<int, int, int>> FindChorusParts()
        {
            List<Tuple<int, int, int>> result = new List<Tuple<int, int, int>>();
            for (int i = 0; i < waveformData.Length; i++)
            {
                float item = waveformData[i];
                int next = -1;
                int lastIndex = i;
                do
                {
                    lastIndex++;
                    var temp = waveformData.Skip(lastIndex).ToList();
                    next = temp.IndexOf(item);
                    if (next == 0)
                        continue;
                    if (next > -1)
                    {
                        next += lastIndex;
                        lastIndex = next;
                        int count = compare(i, next);
                        if (count > 10)
                        {
                            result.Add(new Tuple<int, int, int>(i, next, count));
                            //Console.WriteLine($"Found {i} and {next} length of {count}");
                        }
                    }
                } while (next >= 0);
            }
            return result;
        }

        int compare(int sourceIndex, int nextFoundIndex)
        {
            bool same = false;
            int offset = 0;
            for (int i = sourceIndex; i < waveformData.Length; i++, offset++)
            {
                int i1 = sourceIndex + offset;
                int i2 = nextFoundIndex + offset;
                if (i1 < waveformData.Length && i2 < waveformData.Length)
                {
                    same = waveformData[sourceIndex + offset] == waveformData[nextFoundIndex + offset];
                    if (!same)
                        break;
                }
                else break;
            }

            return offset;
        }
    }
}
