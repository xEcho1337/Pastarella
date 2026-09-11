using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.NtDll;

namespace Pastarella.Core.Windows.Native;

internal static class NtDll
{
    internal static class Wrappers
    {
        public class SystemProcessInformation(SafeHGlobalHandle buffer, SYSTEM_PROCESS_INFORMATION[] processes) : IDisposable
        {
            private readonly SafeHGlobalHandle buffer = buffer;
            public SYSTEM_PROCESS_INFORMATION[] Processes { get; } = processes;

            public void Dispose() => buffer.Dispose();

            public static SystemProcessInformation Get()
            {
                var status = NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemProcessInformation, SafeHGlobalHandle.Null, 0, out uint returnLength);
                if (status != NTStatus.STATUS_INFO_LENGTH_MISMATCH)
                    throw new Exception("Failed to get buffer size");

                var buffer = new SafeHGlobalHandle(returnLength);
                status = NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemProcessInformation, buffer, returnLength, out _);
                if (status != NTStatus.STATUS_SUCCESS)
                    throw new Exception($"Failed to get processes: {status}");

                List<SYSTEM_PROCESS_INFORMATION> list = [];
                nint ptr = buffer.DangerousGetHandle();
                while (true)
                {
                    var process = ptr.ToStructure<SYSTEM_PROCESS_INFORMATION>();
                    list.Add(process);

                    if (process.NextEntryOffset == 0)
                        break;

                    ptr = ptr.Offset(process.NextEntryOffset);
                }

                return new(buffer, [.. list]);
            }
        }

        public static bool IsWOW64(HPROCESS handle)
        {
            using var buffer = new SafeHGlobalHandle(nuint.Size);
            var status = NtQueryInformationProcess(handle, PROCESSINFOCLASS.ProcessWow64Information, buffer, (uint)nuint.Size, out _);
            if (status != NTStatus.STATUS_SUCCESS)
                throw new Exception($"Failed to get WOW64: {status}");

            return buffer.ToType<nuint>() != 0;
        }
    }

    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    public const uint PROCESS_VM_READ = 0x10;

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_PROCESS_INFORMATION
    {
        public uint NextEntryOffset;
        public uint NumberOfThreads;
        public ulong WorkingSetPrivateSize;
        public uint HardFaultCount;
        public uint NumberOfThreadsHighWatermark;
        public ulong CycleTime;
        public long CreateTime;
        public long UserTime;
        public long KernelTime;
        public UNICODE_STRING ImageName;
        public long BasePriority;
        public nint UniqueProcessId;
        public nint InheritedFromUniqueProcessId;
        public uint HandleCount;
        public uint SessionId;
        public nuint UniqueProcessKey;
        public nuint PeakVirtualSize;
        public nuint VirtualSize;
        public uint PageFaultCount;
        public nuint PeakWorkingSetSize;
        public nuint WorkingSetSize;
        public nuint QuotaPeakPagedPoolUsage;
        public nuint QuotaPagedPoolUsage;
        public nuint QuotaPeakNonPagedPoolUsage;
        public nuint QuotaNonPagedPoolUsage;
        public nuint PagefileUsage;
        public nuint PeakPagefileUsage;
        public nuint PrivatePageCount;
        public long ReadOperationCount;
        public long WriteOperationCount;
        public long OtherOperationCount;
        public long ReadTransferCount;
        public long WriteTransferCount;
        public long OtherTransferCount;
        public SYSTEM_THREAD_INFORMATION Threads;
    }

    [DllImport("ntdll.dll", ExactSpelling = true)]
    public static extern NTStatus NtOpenProcess(out HPROCESS ProcessHandle, ACCESS_MASK DesiredAccess, OBJECT_ATTRIBUTES ObjectAttributes, CLIENT_ID ClientId);
}

