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
        private struct Batch 
        {
            public int count;
            public List<double> times;
            public Stopwatch watch;
        }

        private static Stack<Batch> batches = new Stack<Batch>();
        private static Stack<Stopwatch> watches = new Stack<Stopwatch>();

        public static void Start(Engine.Logger logger, string log, params object?[] strings)
        {
            logger.Log(Engine.Logger.LogLevel.Info, log, strings);

            watches.Push(Stopwatch.StartNew());
        }

        public static void End(Engine.Logger logger, string log)
        {
            Stopwatch watch = watches.Pop();
            watch.Stop();

            logger.Log(Engine.Logger.LogLevel.Info, "{0} {1}s.", log, watch.Elapsed.TotalSeconds);
        }

        public static void StartBatch(string log, params object?[] strings)
        {
            Batch b = new Batch()
            {
                count = 0,
                times = new List<double>(),
                watch = Stopwatch.StartNew()
            };

            Console.WriteLine(string.Format(log, strings));

            batches.Push(b);
        }

        public static void AddBatch()
        {
            Batch b = batches.Pop();

            b.count++;
            b.times.Add(b.watch.Elapsed.TotalSeconds);

            batches.Push(b);
        }

        public static void EndBatch(string log)
        {
            Batch b = batches.Pop();

            b.watch.Stop();

            List<double> intervals = new List<double>();
            double previous = 0;
            foreach (double time in b.times)
            {
                double interval = time - previous;
                intervals.Add(interval);

                previous = time;
            }

            intervals.Sort();

            double mean = intervals.Count > 0 ? intervals.Sum() / intervals.Count : 0;
            double median;
            double min = intervals.Count > 0 ? intervals.Min() : 0;
            double max = intervals.Count > 0 ? intervals.Max() : 0;

            if (intervals.Count == 0)
                median = 0;
            else if (intervals.Count == 1)
                median = intervals[0];
            else if (intervals.Count % 2 == 0)
                median = (intervals[intervals.Count / 2] + intervals[intervals.Count / 2 - 1]) / 2.0;
            else median = intervals[intervals.Count / 2];

            Console.WriteLine("{0} Performed batch of {1} in {2:0.00} seconds.\n" +
                "Min: {3:0.0000}s ({7:0.0000} frames)\n" +
                "Max: {4:0.0000}s ({8:0.0000} frames)\n" +
                "Mean: {5:0.0000}s ({9:0.0000} frames)\n" +
                "Median: {6:0.0000}s ({10:0.0000} frames)", 
                log, b.count, b.watch.Elapsed.TotalSeconds, min, max, mean, median, 
                min / Main.FIXED_STEP, max / Main.FIXED_STEP, mean / Main.FIXED_STEP, median / Main.FIXED_STEP);
        }
    }
}
