namespace Pastarella.Core.Models;

public enum PersistenceType
{
    Login,

    Launchd,
    Systemd,
    Cron,
    RcScript,
    Service,
    ScheduledTask,
    KernelExtension,
    SystemExtension,
    StartupFolder,
    ShellProfile,
    LoadableKernelModule,

    // Windows-only
    RegistryKey,
    OfflineRegistry,

    Unknown
}

public enum PersistencePrivilege
{
    // User-mode
    User,
    Admin,

    // Kernel-mode
    Kernel,
}

public enum PersistenceScope
{
    CurrentUser,
    AllUsers,
    System
}

public enum ExecutionTrigger
{
    Boot,
    SystemStartup,
    UserLogin,
    Scheduled,
    ServiceStart,
    NetworkEvent,
    Unknown
}

[Flags]
public enum ServiceKind
{
    UserService,
    SystemService,
    Driver,
    MacAgent,
    MacDaemon,
    MacSystemExtension
}

[Flags]
public enum ServiceType
{
    KernelDriver = 1,
    FileSystemDriver = 2,
    Adapter = 4,
    RecognizerDriver = 8,
    Win32OwnProcess = 16,
    Win32ShareProcess = 32,
    InteractiveProcess = 256,
    MacOSService,
}

public enum ServiceStatus
{
    Stopped = 1,
    StartPending = 2,
    StopPending = 3,
    Running = 4,
    ContinuePending = 5,
    PausePending = 6,
    Paused = 7,
}

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
