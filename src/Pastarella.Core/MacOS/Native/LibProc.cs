using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Pastarella.Core.MacOS.Native;

/// <summary>
/// Generated through ClangSharp
/// </summary>
public static unsafe class LibProc
{
    [DllImport("libc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int proc_listpidspath([NativeTypeName("uint32_t")] uint type, [NativeTypeName("uint32_t")] uint typeinfo, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("uint32_t")] uint pathflags, void* buffer, int buffersize);
    [DllImport("libc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int proc_listpids([NativeTypeName("uint32_t")] uint type, [NativeTypeName("uint32_t")] uint typeinfo, void* buffer, int buffersize);
    [DllImport("libc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int proc_listallpids(void* buffer, int buffersize);
    [DllImport("libc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int proc_pidinfo(int pid, int flavor, [NativeTypeName("uint64_t")] ulong arg, void* buffer, int buffersize);
    [DllImport("libc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int proc_pidfdinfo(int pid, int fd, int flavor, void* buffer, int buffersize);
    [DllImport("libc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int proc_name(int pid, void* buffer, [NativeTypeName("uint32_t")] uint buffersize);
    [DllImport("libc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int proc_regionfilename(int pid, [NativeTypeName("uint64_t")] ulong address, void* buffer, [NativeTypeName("uint32_t")] uint buffersize);
    [DllImport("libc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int proc_pidpath(int pid, void* buffer, [NativeTypeName("uint32_t")] uint buffersize);
}

internal sealed class NativeTypeNameAttribute(string name) : Attribute
{
    public string Name => name;
}

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
[Conditional("DEBUG")]
internal sealed class NativeAnnotationAttribute(string annotation) : Attribute
{
    public string Annotation => annotation;
}
