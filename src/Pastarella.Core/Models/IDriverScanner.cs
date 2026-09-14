namespace Pastarella.Core.Models;

public enum DriverType
{
    // Linux-only
    BuiltinKernelModule,
    KernelModule,

    // Windows-only
    Kernel,
    Filesystem,

    // MacOS-only
    KernelExtension,
    CameraExtension,
    DriverExtension,
    NetworkExtension
}

public record DriverInfo(
    string Name,
    string DisplayName,
    string Identifier,
    DriverType Type,
    ExePath? ExePath,
    string? Version,
    bool Loaded
);

public interface IDriverScanner
{
    IEnumerable<DriverInfo> Scan(IProgress<ScanProgress>? progress = null);
}
