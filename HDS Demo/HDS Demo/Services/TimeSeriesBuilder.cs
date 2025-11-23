using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HDS_Demo.Models;
using System.Threading;

namespace HDS_Demo.Server
{
    public static class TimeSeriesBuilder
    {
        private const int SampleCount = 30;
        private const int SampleIntervalMs = 1000;

        public static FaultTimeSeries Build(CaptureRequest request)
        {
            var ts = new FaultTimeSeries();

            bool wantsCpu = request.Environment.Contains("CPU", StringComparer.OrdinalIgnoreCase);
            bool wantsRam = request.Environment.Contains("RAM", StringComparer.OrdinalIgnoreCase);
            bool wantsGpu = request.Environment.Contains("GPU", StringComparer.OrdinalIgnoreCase);
            bool wantsDisk = request.Environment.Contains("DISK", StringComparer.OrdinalIgnoreCase);
            bool wantsNetwork = request.Environment.Contains("NETWORK", StringComparer.OrdinalIgnoreCase);

            float prevDiskBytes = GetDiskBytes();
            float prevNetBytes = GetNetworkBytes();

            for (int i = 0; i < SampleCount; i++)
            {
                DateTime now = DateTime.UtcNow;

                // ---------------- CPU ----------------
                if (wantsCpu)
                {
                    float cpu = GetCpuUsage();
                    ts.Cpu.Add(new TimedValue<float> { Timestamp = now, Value = cpu });
                }

                // ---------------- RAM ----------------
                if (wantsRam)
                {
                    float ram = GetRamUsageGB();
                    ts.Ram.Add(new TimedValue<float> { Timestamp = now, Value = ram });
                }

                // ---------------- GPU (placeholder) ----------------
                if (wantsGpu)
                {
                    float gpu = GetGpuUsage();  // Preferred: implement with NVML later
                    ts.Gpu.Add(new TimedValue<float> { Timestamp = now, Value = gpu });
                }

                // ---------------- Disk MB/s ----------------
                if (wantsDisk)
                {
                    float currentDisk = GetDiskBytes();
                    float mbps = (currentDisk - prevDiskBytes) / 1024f / 1024f;
                    prevDiskBytes = currentDisk;

                    ts.Disk.Add(new TimedValue<float> { Timestamp = now, Value = mbps });
                }

                // ---------------- Network KB/s ----------------
                if (wantsNetwork)
                {
                    float currentNet = GetNetworkBytes();
                    float kbps = (currentNet - prevNetBytes) / 1024f;
                    prevNetBytes = currentNet;

                    ts.Network.Add(new TimedValue<float> { Timestamp = now, Value = kbps });
                }

                Thread.Sleep(SampleIntervalMs);
            }

            return ts;
        }

        // ============================================================
        // METRIC COLLECTION HELPERS
        // ============================================================

        private static PerformanceCounter cpuCounter =
            new PerformanceCounter("Processor", "% Processor Time", "_Total");

        private static float GetCpuUsage()
        {
            try { return cpuCounter.NextValue(); }
            catch { return 0; }
        }

        private static float GetRamUsageGB()
        {
            float totalMB = MemoryInfo.GetTotalMemoryMB();
            float availableMB = MemoryInfo.GetAvailableMemoryMB();

            float usedMB = totalMB - availableMB;
            return usedMB / 1024f;   // GB
        }


        private static float GetTotalMemoryMB()
        {
            return MemoryInfo.GetTotalMemoryMB();
        }


        private static float GetGpuUsage()
        {
            // Placeholder (0).  
            // REAL implementation possible with NVML for NVIDIA GPUs.
            return 0;
        }

        private static float GetDiskBytes()
        {
            try
            {
                var counter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
                return counter.NextValue();
            }
            catch { return 0; }
        }

        private static float GetNetworkBytes()
        {
            try
            {
                var counter = new PerformanceCounter("Network Interface", "Bytes Total/sec");
                return counter.NextValue();
            }
            catch { return 0; }
        }
    }
}
