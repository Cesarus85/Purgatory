using System;
using System.Globalization;
using System.IO;

namespace QuestDemonMR
{
    // Pure, allocation-free sampling; runnable without Unity or a headset.
    public sealed class DiagnosticWindow
    {
        private readonly int[] _histogram = new int[512];
        public int Count { get; private set; }
        public int OverBudget { get; private set; }
        public int CpuCount { get; private set; }
        public int GpuCount { get; private set; }
        private double _sum, _cpu, _gpu;
        public double Maximum { get; private set; }
        public double Mean => Count == 0 ? 0 : _sum / Count;
        public double CpuMean => CpuCount == 0 ? -1 : _cpu / CpuCount;
        public double GpuMean => GpuCount == 0 ? -1 : _gpu / GpuCount;
        public static bool Positive(double value) => value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);
        public void Add(double frameMs, double cpuMs, double gpuMs, double budgetMs)
        {
            if (!Positive(frameMs) || !Positive(budgetMs)) return;
            Count++; _sum += frameMs; Maximum = Math.Max(Maximum, frameMs);
            if (frameMs > budgetMs) OverBudget++;
            _histogram[Math.Min(511, Math.Max(0, (int)Math.Ceiling(Math.Min(frameMs, 128) * 4) - 1))]++;
            if (Positive(cpuMs)) { _cpu += cpuMs; CpuCount++; }
            if (Positive(gpuMs)) { _gpu += gpuMs; GpuCount++; }
        }
        public double Percentile95()=>Percentile(.95);
        public double Percentile99()=>Percentile(.99);
        double Percentile(double quantile)
        {
            if (Count == 0) return 0;
            var rank = (int)Math.Ceiling(Count * quantile); var sum = 0;
            for (var i = 0; i < _histogram.Length; i++)
                if ((sum += _histogram[i]) >= rank) return i == 511 ? Maximum : (i + 1) * .25;
            return Maximum;
        }
        public void Reset()
        {
            Array.Clear(_histogram, 0, _histogram.Length);
            Count = OverBudget = CpuCount = GpuCount = 0; _sum = _cpu = _gpu = Maximum = 0;
        }
    }

    public static class DiagnosticBenchmarkPlan
    {
        public const int Seed = 170917;
        public const double Duration = 60;
        public static bool CanStart(bool active, bool running, int wave, int enemies, bool room, bool head, bool diagnostics) =>
            !active && !running && wave == 0 && enemies == 0 && room && head && diagnostics;
        public static int Phase(double seconds) => seconds < 0 || seconds >= Duration || double.IsNaN(seconds) ? -1 :
            seconds < 10 ? 0 : seconds < 20 ? 1 : seconds < 30 ? 2 : seconds < 45 ? 3 : 4;
        public static string Label(int phase) => phase switch
        { 0 => "baseline", 1 => "one_portal", 2 => "two_portals", 3 => "actors", 4 => "combat_fx", _ => "manual" };
    }

    public sealed class DiagnosticLog : IDisposable
    {
        public const int SampleLimit = 1200, EventLimit = 2048;
        private readonly StreamWriter _samples, _events;
        public string DirectoryPath { get; }
        public int Samples { get; private set; }
        public int Events { get; private set; }
        public bool Closed { get; private set; }
        public DiagnosticLog(string root, string metadata)
        {
            DirectoryPath = Path.Combine(root, "v17-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(Path.Combine(DirectoryPath, "metadata.txt"), metadata);
            _samples = new StreamWriter(Path.Combine(DirectoryPath, "frames.csv"));
            _events = new StreamWriter(Path.Combine(DirectoryPath, "events.csv"));
            _samples.WriteLine("elapsed_s,phase,overlay,frames,frame_mean_ms,frame_p95_ms,frame_max_ms,cpu_mean_ms,gpu_mean_ms,over_budget,refresh_hz,unity_allocated_bytes,managed_bytes,gc_collections,depth_probes,mesh_bakes,triangle_tests,flight_blocks,visible_portals,portal_pixels,dropped_frames,cpu_samples,gpu_samples,refresh_source,living_enemies,frame_p99_ms");
            _events.WriteLine("elapsed_s,event,detail");
        }
        public bool Sample(string csv)
        {
            if (Closed || Samples >= SampleLimit) return false;
            _samples.WriteLine(csv); Samples++; return true;
        }
        public void Event(double elapsed, string kind, string detail)
        {
            if (Closed || Events >= EventLimit) return;
            _events.WriteLine(elapsed.ToString("F3", CultureInfo.InvariantCulture) + "," + Safe(kind) + "," + Safe(detail)); Events++;
        }
        private static string Safe(string text) => (text ?? "").Replace(',', ';').Replace('\n', ' ').Replace('\r', ' ');
        public void Dispose()
        {
            if (Closed) return;
            Closed = true;
            try { _samples.Dispose(); } finally { _events.Dispose(); }
        }
    }
}
