using System;
using System.Runtime.InteropServices;

public static class MemoryInfo
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    public static float GetTotalMemoryMB()
    {
        var mem = new MEMORYSTATUSEX();
        if (!GlobalMemoryStatusEx(mem))
            return 0;

        return mem.ullTotalPhys / 1024f / 1024f;
    }

    public static float GetAvailableMemoryMB()
    {
        var mem = new MEMORYSTATUSEX();
        if (!GlobalMemoryStatusEx(mem))
            return 0;

        return mem.ullAvailPhys / 1024f / 1024f;
    }
}
