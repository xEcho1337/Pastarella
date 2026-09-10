using System.Runtime.InteropServices;

namespace Pastarella.Core.MacOS.Native;

public static unsafe class SysCtl
{
    [DllImport("libc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int sysctl(int* param0, [NativeTypeName("u_int")] uint param1, void* param2, [NativeTypeName("size_t *")] nuint* oldlenp, void* param4, [NativeTypeName("size_t")] nuint newlen);

    [DllImport("libc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int sysctlbyname([NativeTypeName("const char *")] sbyte* param0, void* param1, [NativeTypeName("size_t *")] nuint* oldlenp, void* param3, [NativeTypeName("size_t")] nuint newlen);

    [DllImport("libc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int sysctlnametomib([NativeTypeName("const char *")] sbyte* param0, int* param1, [NativeTypeName("size_t *")] nuint* sizep);
}
