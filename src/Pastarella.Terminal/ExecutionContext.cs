using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Pastarella.Core.Common;
using Pastarella.Core.Models;

namespace Pastarella.Terminal;

public static class ExecutionContext
{
    public static readonly OperatingSystems OS = GetOperatingSystem();

    public static readonly IAnalysisDispatcher Dispatcher = GetAnalyzer();
    public static readonly IForensicScanner ForensicScanner = GetForensicScanner();
    public static readonly IPersistenceScanner PersistenceScanner = GetPersistenceScanner();
    public static readonly INetworkScanner NetworkScanner = GetNetworkScanner();
    public static readonly IDriverScanner DriverScanner = GetDriverScanner();
    public static readonly IServiceScanner ServiceScanner = GetServiceScanner();
    public static readonly ICommandHistoryScanner CommandHistoryScanner = GetCommandHistoryScanner();

    public static readonly ConcurrentDictionary<string, string> HashCache = new();

    private static OperatingSystems GetOperatingSystem()
    {
        if (OperatingSystem.IsWindows())
            return OperatingSystems.Windows;
        if (OperatingSystem.IsMacOS())
            return OperatingSystems.MacOS;
        if (OperatingSystem.IsLinux())
            return OperatingSystems.Linux;
        if (OperatingSystem.IsFreeBSD())
            return OperatingSystems.FreeBSD;

        throw new NotImplementedException($"{RuntimeInformation.OSDescription} is currently not supported");
    }

    private static IForensicScanner GetForensicScanner() => OS switch
    {
        OperatingSystems.Windows => new Core.Windows.ForensicScanner(),
        OperatingSystems.MacOS => new Core.MacOS.ForensicScanner(),
        OperatingSystems.Linux => new Core.Linux.ForensicScanner(),
        OperatingSystems.FreeBSD => new Core.FreeBSD.ForensicScanner(),
        _ => throw new NotImplementedException($"{RuntimeInformation.OSDescription} is currently not supported.")
    };

    public static IAnalysisDispatcher GetAnalyzer() => OS switch
    {
        OperatingSystems.Windows => new Core.Windows.AnalysisDispatcher(),
        OperatingSystems.MacOS => new Core.MacOS.AnalysisDispatcher(),
        OperatingSystems.Linux => new Core.Linux.AnalysisDispatcher(),
        OperatingSystems.FreeBSD => new Core.FreeBSD.AnalysisDispatcher(),
        _ => throw new NotImplementedException($"{RuntimeInformation.OSDescription} is currently not supported.")
    };

    public static IPersistenceScanner GetPersistenceScanner() => OS switch
    {
        OperatingSystems.Windows => new Core.Windows.PersistenceScanner(),
        OperatingSystems.MacOS => new Core.MacOS.PersistenceScanner(),
        OperatingSystems.Linux => new Core.Linux.PersistenceScanner(),
        OperatingSystems.FreeBSD => new Core.FreeBSD.PersistenceScanner(),
        _ => throw new NotImplementedException($"{RuntimeInformation.OSDescription} is currently not supported.")
    };


    private static IDriverScanner GetDriverScanner() => OS switch
    {
        OperatingSystems.Windows => new Core.Windows.DriverScanner(),
        OperatingSystems.MacOS => new Core.MacOS.DriverScanner(),
        OperatingSystems.Linux => new Core.Linux.DriverScanner(),
        OperatingSystems.FreeBSD => new Core.FreeBSD.DriverScanner(),
        _ => throw new NotImplementedException($"{RuntimeInformation.OSDescription} is currently not supported.")
    };

    private static INetworkScanner GetNetworkScanner() => OS switch
    {
        OperatingSystems.Windows => new Core.Windows.NetworkScanner(),
        OperatingSystems.MacOS => new Core.MacOS.NetworkScanner(),
        OperatingSystems.Linux => new Core.Linux.NetworkScanner(),
        OperatingSystems.FreeBSD => new Core.FreeBSD.NetworkScanner(),
        _ => throw new NotImplementedException($"{RuntimeInformation.OSDescription} is currently not supported.")
    };


    private static IServiceScanner GetServiceScanner() => OS switch
    {
        OperatingSystems.Windows => new Core.Windows.ServiceScanner(),
        OperatingSystems.MacOS => new Core.MacOS.ServiceScanner(),
        OperatingSystems.Linux => new Core.Linux.ServiceScanner(),
        OperatingSystems.FreeBSD => new Core.FreeBSD.ServiceScanner(),
        _ => throw new NotImplementedException($"{RuntimeInformation.OSDescription} is currently not supported.")
    };

    private static ICommandHistoryScanner GetCommandHistoryScanner() => OS switch
    {
        OperatingSystems.Windows => new Core.Windows.CommandHistoryScanner(),
        OperatingSystems.MacOS or OperatingSystems.Linux or OperatingSystems.FreeBSD => new Core.Unix.CommandHistoryScanner(),
        _ => throw new NotImplementedException($"{RuntimeInformation.OSDescription} is currently not supported.")
    };
}
