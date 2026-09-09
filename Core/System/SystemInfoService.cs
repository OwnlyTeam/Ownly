using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Ownly.Core.System;

public sealed class SystemInfoService
{
    public SystemSnapshot ReadSnapshot()
    {
        var build = Environment.OSVersion.Version.Build;
        var windowsName = build >= 22000 ? "Windows 11" : "Windows 10";
        var memory = new MemoryStatus();
        _ = GlobalMemoryStatusEx(ref memory);

        var systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
        var drive = DriveInfo.GetDrives()
            .FirstOrDefault(item => item.IsReady && string.Equals(item.Name, systemDrive, StringComparison.OrdinalIgnoreCase));

        var storage = drive is null
            ? "Unavailable"
            : $"{ToGigabytes((ulong)drive.AvailableFreeSpace):0.0} GB free";
        var storageDetail = drive is null
            ? "Storage details unavailable"
            : $"of {ToGigabytes((ulong)drive.TotalSize):0.0} GB on {drive.Name.TrimEnd('\\')}";

        return new SystemSnapshot(
            Environment.MachineName,
            $"{windowsName} · Build {build}",
            $"{Environment.ProcessorCount} logical processors",
            memory.TotalPhysicalMemory == 0 ? "Unavailable" : $"{ToGigabytes(memory.TotalPhysicalMemory):0.0} GB",
            storage,
            storageDetail);
    }

    private static double ToGigabytes(ulong bytes) => bytes / 1024d / 1024d / 1024d;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MemoryStatus
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysicalMemory;
        public ulong AvailablePhysicalMemory;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;

        public MemoryStatus()
        {
            Length = (uint)Marshal.SizeOf<MemoryStatus>();
            MemoryLoad = 0;
            TotalPhysicalMemory = 0;
            AvailablePhysicalMemory = 0;
            TotalPageFile = 0;
            AvailablePageFile = 0;
            TotalVirtual = 0;
            AvailableVirtual = 0;
            AvailableExtendedVirtual = 0;
        }
    }
}
