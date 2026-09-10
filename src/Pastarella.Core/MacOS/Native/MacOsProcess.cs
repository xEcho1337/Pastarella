using System.Text;

namespace Pastarella.Core.MacOS.Native;

public static unsafe class MacOsProcess
{
    private const int CtlKern = 1;
    private const int KernProcArgs2 = 49;
    private const int ProcPidPathBufSize = 4096;

    public static string? GetCommandLine(int pid)
    {
        int[] mib = [CtlKern, KernProcArgs2, pid];

        nuint len = 0; // get size
        fixed (int* mibPtr = mib)
        {
            if (SysCtl.sysctl(mibPtr, 3, null, &len, null, 0) != 0)
                return null;
        }

        if (len is 0 or > 1024 * 1024)
            return null;

        // read
        byte[] buf = new byte[len];
        nuint actualLen = len;
        fixed (int* mibPtr = mib)
        fixed (byte* bufPtr = buf)
        {
            if (SysCtl.sysctl(mibPtr, 3, bufPtr, &actualLen, null, 0) != 0)
                return null;
        }

        int argc = BitConverter.ToInt32(buf, 0);
        if (argc is <= 0 or > 4096)
            return null;

        // layout: [argc][exec_path\0][argv0\0]...
        string[] parts = Encoding.UTF8
            .GetString(buf, 4, (int)actualLen - 4)
            .Split('\0', StringSplitOptions.RemoveEmptyEntries);

        return parts.Length <= 1 ? null : string.Join(' ', parts.Skip(1).Take(argc));
    }

    public static string? GetExePath(int pid)
    {
        byte[] buf = new byte[ProcPidPathBufSize];
        fixed (byte* ptr = buf)
        {
            int ret = LibProc.proc_pidpath(pid, ptr, (uint)buf.Length);
            if (ret <= 0)
                return null;
            return Encoding.UTF8.GetString(buf, 0, ret);
        }
    }

}
