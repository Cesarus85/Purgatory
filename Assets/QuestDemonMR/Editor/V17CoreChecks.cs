using System;
using System.IO;

namespace QuestDemonMR.Editor
{
    // Shared by the editor suite and the license-independent .NET test executable.
    public static class V17CoreChecks
    {
        public static void Run(Action<bool, string> check, string output)
        {
            var window = new DiagnosticWindow();
            check(window.CpuMean == -1 && window.GpuMean == -1, "unavailable timing is not reported as zero");
            window.Add(double.NaN, 1, 1, 14); window.Add(double.PositiveInfinity, 1, 1, 14);
            window.Add(0, 1, 1, 14); window.Add(-1, 1, 1, 14); window.Add(12, 1, 1, double.NaN);
            check(window.Count == 0, "invalid duration/budget samples are excluded");
            window.Add(10, 2, -1, 14); window.Add(20, -1, 4, 14);
            check(window.Count == 2 && window.Mean == 15 && window.Maximum == 20, "frame count mean maximum");
            check(window.CpuMean == 2 && window.GpuMean == 4 && window.CpuCount == 1 && window.GpuCount == 1,
                "CPU and GPU have independent valid-sample denominators");
            check(window.OverBudget == 1 && window.Percentile95() == 20, "budget and percentile include slow frames");
            window.Reset(); check(window.Count == 0 && window.OverBudget == 0 && window.CpuCount == 0 && window.GpuMean == -1, "window reset clears all statistics");
            for (var i = 0; i < 95; i++) window.Add(10.01, 0, double.NaN, 14);
            for (var i = 0; i < 5; i++) window.Add(40, -1, 0, 14);
            check(window.Percentile95() == 10.25 && window.Maximum == 40, "P95 nearest rank with documented quarter-ms upper bin");
            check(window.CpuCount == 0 && window.GpuCount == 0, "zero and NaN provider timings remain unavailable");
            window.Reset(); window.Add(2000, -1, -1, 14);
            check(window.Percentile95() == 2000, "overflow histogram retains long stalls instead of clamping them away");
            window.Reset();
            for (var i = 0; i < 100; i++) window.Add(12, 5, 6, 14);
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 10000; i++) window.Add(12, 5, 6, 14);
            check(GC.GetAllocatedBytesForCurrentThread() == allocated, "sampling hot path allocates no managed memory after warmup");

            foreach (var test in new[] { (0d,0), (9.999,0), (10d,1), (19.999,1), (20d,2), (29.999,2),
                (30d,3), (44.999,3), (45d,4), (59.999,4), (60d,-1), (-1d,-1), (double.NaN,-1) })
                check(DiagnosticBenchmarkPlan.Phase(test.Item1) == test.Item2, "benchmark phase boundary " + test.Item1);
            check(DiagnosticBenchmarkPlan.CanStart(false, false, 0, 0, true, true, true), "benchmark accepts an idle empty scanned room");
            check(!DiagnosticBenchmarkPlan.CanStart(true, false, 0, 0, true, true, true), "benchmark refuses reentry");
            check(!DiagnosticBenchmarkPlan.CanStart(false, true, 0, 0, true, true, true), "benchmark refuses a running game");
            check(!DiagnosticBenchmarkPlan.CanStart(false, false, 1, 0, true, true, true), "benchmark preserves an existing paused round");
            check(!DiagnosticBenchmarkPlan.CanStart(false, false, 0, 1, true, true, true), "benchmark preserves existing enemies");
            check(!DiagnosticBenchmarkPlan.CanStart(false, false, 0, 0, false, true, true), "benchmark refuses missing room data");
            check(!DiagnosticBenchmarkPlan.CanStart(false, false, 0, 0, true, false, true), "benchmark refuses missing head pose");
            check(!DiagnosticBenchmarkPlan.CanStart(false, false, 0, 0, true, true, false), "benchmark refuses missing recorder");

            var log = new DiagnosticLog(output, "synthetic_core_test=true\nnot_a_headset_measurement=true\n");
            for (var i = 0; i < DiagnosticLog.SampleLimit + 3; i++) log.Sample("synthetic");
            for (var i = 0; i < DiagnosticLog.EventLimit + 3; i++) log.Event(i, "test", "comma,newline\nclean");
            check(log.Samples == DiagnosticLog.SampleLimit && log.Events == DiagnosticLog.EventLimit, "logs have bounded sample and event counts");
            log.Dispose(); log.Dispose();
            check(!log.Sample("closed"), "closed recorder refuses new samples and disposes idempotently");
            var lines = File.ReadAllLines(Path.Combine(log.DirectoryPath, "frames.csv"));
            check(lines.Length == DiagnosticLog.SampleLimit + 1 && lines[0].Split(',').Length == 26,
                "CSV flushes all bounded rows and declares the complete schema");
            var events = File.ReadAllLines(Path.Combine(log.DirectoryPath, "events.csv"));
            check(events.Length == DiagnosticLog.EventLimit + 1 && events[1].Split(',').Length == 3,
                "event commas and newlines cannot corrupt row structure");
            var second = new DiagnosticLog(output, "synthetic_second=true");
            check(second.DirectoryPath != log.DirectoryPath, "sessions never overwrite earlier measurements");
            second.Dispose();
        }
    }
}
