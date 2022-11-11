using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public static class ProfilingHelper
    {
        private static Stack<Stopwatch> watches = new Stack<Stopwatch>();

        public static void Start(string log, params object?[] strings)
        {
            Console.WriteLine(string.Format(log, strings));

            watches.Push(Stopwatch.StartNew());
        }

        public static void End(string log)
        {
            Stopwatch watch = watches.Pop();
            watch.Stop();

            Console.WriteLine("{0} {1}s.", log, watch.Elapsed.TotalSeconds);
        }
    }
}
