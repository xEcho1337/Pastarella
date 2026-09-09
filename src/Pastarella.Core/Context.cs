using System.Runtime.InteropServices;
using Pastarella.Core.Models;
using Pastarella.Core.Common;

namespace Pastarella.Core;

public class Context
{
    private enum OS
    {
        Windows,
        MacOS,
        Linux,
        FreeBSD,
    }

    private static OS GetOS()
    {
        if (OperatingSystem.IsWindows())
            return OS.Windows;
        if (OperatingSystem.IsMacOS())
            return OS.MacOS;
        if (OperatingSystem.IsLinux())
            return OS.Linux;
        if (OperatingSystem.IsFreeBSD())
            return OS.FreeBSD;

        throw new NotImplementedException($"{RuntimeInformation.OSDescription} is currently not supported");
    }

    private static readonly OS Os = GetOS();

    public readonly IForensicScanner? ForensicScanner;
    public readonly IPersistenceScanner? PersistenceScanner;
    public readonly INetworkScanner? NetworkScanner;
    public readonly IDriverScanner? DriverScanner;
    public readonly IServiceScanner? ServiceScanner;
    public readonly ICommandHistoryScanner? CommandHistoryScanner;

    public Context()
    {
        switch (Os)
        {
            case OS.Windows:
                ForensicScanner = new Core.Windows.ForensicScanner();
                PersistenceScanner = new Core.Windows.PersistenceScanner();
                NetworkScanner = new Core.Windows.NetworkScanner();
                DriverScanner = new Core.Windows.DriverScanner();
                ServiceScanner = new Core.Windows.ServiceScanner();
                CommandHistoryScanner = new Core.Windows.CommandHistoryScanner();
                break;
            case OS.MacOS:
                ForensicScanner = new Core.MacOS.ForensicScanner();
                PersistenceScanner = new Core.MacOS.PersistenceScanner();
                NetworkScanner = new Core.MacOS.NetworkScanner();
                DriverScanner = new Core.MacOS.DriverScanner();
                ServiceScanner = new Core.MacOS.ServiceScanner();
                CommandHistoryScanner = new Core.Unix.CommandHistoryScanner();
                break;
            case OS.Linux:
                Core.Linux.Context.Setup();

                ForensicScanner = new Core.Linux.ForensicScanner();
                PersistenceScanner = new Core.Linux.PersistenceScanner();
                NetworkScanner = new Core.Linux.NetworkScanner();
                DriverScanner = new Core.Linux.DriverScanner();
                ServiceScanner = null;
                CommandHistoryScanner = new Core.Unix.CommandHistoryScanner();
                break;
            case OS.FreeBSD:
                ForensicScanner = new Core.FreeBSD.ForensicScanner();
                PersistenceScanner = new Core.FreeBSD.PersistenceScanner();
                NetworkScanner = null;
                DriverScanner = new Core.FreeBSD.DriverScanner();
                ServiceScanner = null;
                CommandHistoryScanner = new Core.Unix.CommandHistoryScanner();
                break;
            default:
                throw new NotImplementedException($"{RuntimeInformation.OSDescription} is currently not supported");
        }
    }
}
