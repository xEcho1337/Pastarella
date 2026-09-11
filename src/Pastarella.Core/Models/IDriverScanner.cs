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
    string? ExecutablePath,
    string? Version,
    bool Loaded,
    string? Sha256,
    string? Signer
);

public interface IDriverScanner
{
    IEnumerable<DriverInfo> Scan(IProgress<ScanProgress>? progress = null);
}
