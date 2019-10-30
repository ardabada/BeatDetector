using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceFinder
{
    class Program
    {
        static int[] arr = { 0, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 4, 5, 2, 3, 4, 8 };

        public static void Main(String[] args)
        {

            for (int i = 0; i < arr.Length; i++)
            {
                int item = arr[i];
                int next = -1;
                int lastIndex = i;
                do
                {
                    lastIndex++;
                    var temp = arr.Skip(lastIndex).ToList();
                    next = temp.IndexOf(item);
                    if (next == 0)
                        continue;
                    if (next > -1)
                    {
                        next += lastIndex;
                        lastIndex = next;
                        int count = compare(i, next);
                        if (count > 2)
                        {
                            Console.WriteLine($"Found {i} and {next} length of {count}");
                        }
                    }
                } while (next >= 0);
            }

            Console.WriteLine("Finished");
            Console.ReadLine();
        }

        static int compare(int sourceIndex, int nextFoundIndex)
        {
            bool same = false;
            int offset = 0;
            for (int i = sourceIndex; i < arr.Length; i++, offset++)
            {
                same = arr[sourceIndex + offset] == arr[nextFoundIndex + offset];
                if (!same)
                    break;
            }

            return offset;
        }
    }
}
