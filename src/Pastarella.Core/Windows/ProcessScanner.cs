using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Pastarella.Core.Models;

using static Vanara.PInvoke.NtDll;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Shell32;
using Vanara.PInvoke;
using System.Security.Cryptography;
using Vanara.InteropServices;
using Vanara.Extensions;
using System.Runtime.InteropServices;

namespace Pastarella.Core.Windows;

public class ProcessScanner : IProcessScanner
{
    private static HPROCESS? GetProcessHandle(int pid, ACCESS_MASK accessMask)
    {
        var objAttr = new OBJECT_ATTRIBUTES()
        {
            objectName = 0 /* null */,
        };

        var clientId = new CLIENT_ID()
        {
            UniqueProcess = pid,
            UniqueThread = 0 /* null */,
        };

        if (Native.NtDll.NtOpenProcess(out var handle, accessMask, objAttr, clientId) != NTStatus.STATUS_SUCCESS)
            return null;

        return handle;
    }

    private static string? GetProcessFilePath(HPROCESS handle)
    {
        var status = NtQueryInformationProcess(handle, PROCESSINFOCLASS.ProcessImageFileName, SafeHGlobalHandle.Null, 0, out uint returnLength);
        if (status != NTStatus.STATUS_INFO_LENGTH_MISMATCH)
            throw new Exception("Failed to get buffer size");

        using var buffer = new SafeHGlobalHandle(returnLength);
        status = NtQueryInformationProcess(handle, PROCESSINFOCLASS.ProcessImageFileName, buffer, returnLength, out returnLength);
        if (status != NTStatus.STATUS_SUCCESS)
            throw new Exception($"Failed to get processes: {status}");

        return buffer.DangerousGetHandle().ToStructure<UNICODE_STRING>().ToString();
    }

    private static string[]? GetCommandLine(HPROCESS normalHandle)
    {
        if (Native.NtDll.Wrappers.IsWOW64(normalHandle))
        {
            // TODO: do WOW64
            return null;
        }
        else
        {
            var result = NtQueryInformationProcess<PROCESS_BASIC_INFORMATION>(normalHandle, PROCESSINFOCLASS.ProcessBasicInformation);
            if (result == null || result.Value.PebBaseAddress == 0 /* null */)
                return null;

            if (GetProcessHandle((int)result.Value.UniqueProcessId, Native.NtDll.PROCESS_VM_READ) is not HPROCESS memoryHandle)
                return null;

            using var pebBuffer = new SafeHGlobalHandle(Marshal.SizeOf<PEB>());
            if (!ReadProcessMemory(memoryHandle, result.Value.PebBaseAddress, pebBuffer, Marshal.SizeOf<PEB>(), out _))
                return null;
            var peb = pebBuffer.DangerousGetHandle().ToStructure<PEB>();

            using var processParamsBuffer = new SafeHGlobalHandle(Marshal.SizeOf<RTL_USER_PROCESS_PARAMETERS>());
            if (!ReadProcessMemory(memoryHandle, peb.ProcessParameters, processParamsBuffer, Marshal.SizeOf<RTL_USER_PROCESS_PARAMETERS>(), out _))
                return null;
            var processParams = processParamsBuffer.DangerousGetHandle().ToStructure<RTL_USER_PROCESS_PARAMETERS>();

            string[] cmdline = [.. CommandLineToArgvW(processParams.CommandLine.ToString(memoryHandle)).Skip(1).Select(l => l!)];
            return cmdline.Length == 0 ? null : cmdline;
        }
    }

    public IEnumerable<ProcessInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        var info = Native.NtDll.Wrappers.SystemProcessInformation.Get();

        var result = new List<ProcessInfo>(info.Processes.Length);
        int done = 0;

        foreach (var process in info.Processes)
        {
            Dictionary<string, object> metadata = [];
            DateTime startTime = DateTimeOffset.FromFileTime(process.CreateTime).UtcDateTime;

            int pid = (int)process.UniqueProcessId;
            string name = process.ImageName.ToString();

            if (GetProcessHandle(pid, Native.NtDll.PROCESS_QUERY_LIMITED_INFORMATION) is not HPROCESS handle)
            {
                result.Add(new ProcessInfo(pid)
                {
                    ExePath = new FakeExePath(name),
                    CommandArgs = null,
                    StartTime = startTime,
                    Metadata = metadata,
                });
                progress?.Report(new ScanProgress(++done, info.Processes.Length));

                continue;
            }

            ExePath exePath;
            if (GetProcessFilePath(handle) is string ntPath && ntPath.Length > 0)
            {
                exePath = new(ntPath);

                if (exePath.Exist())
                {
                    var exeInfo = FileVersionInfo.GetVersionInfo(exePath.NormalizedValue);

                    if (exeInfo.CompanyName != null)
                        metadata["Company"] = exeInfo.CompanyName;

                    if (exeInfo.ProductName != null)
                        metadata["Product"] = exeInfo.ProductName;
                }
            }
            else
            {
                exePath = new FakeExePath(name);
            }

            result.Add(new ProcessInfo(pid)
            {
                Metadata = metadata,
                CommandArgs = GetCommandLine(handle),
                ExePath = exePath,
                StartTime = startTime
            });
            progress?.Report(new ScanProgress(++done, info.Processes.Length));
        }

        return result;
    }
}
