using System.Runtime.InteropServices;
using Pastarella.Core.Models;

namespace Pastarella.Core;

public class Context
{
    public enum OS
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

    public static readonly OS Os = GetOS();

    public readonly IProcessScanner? ProcessScanner;
    public readonly IUserScanner? UserScanner;
    public readonly IStorageScanner? StorageScanner;
    public readonly IPersistenceScanner? PersistenceScanner;
    public readonly INetworkScanner? NetworkScanner;
    public readonly IDriverScanner? DriverScanner;
    public readonly IServiceScanner? ServiceScanner;
    public readonly ICommandHistoryScanner? CommandHistoryScanner;
    public readonly IContainerScanner? ContainerScanner;

    public Context()
    {
        switch (Os)
        {
            case OS.Windows:
                ProcessScanner = new Windows.ProcessScanner();
                UserScanner = new Windows.UserScanner();
                StorageScanner = new Windows.StorageScanner();
                PersistenceScanner = new Windows.PersistenceScanner();
                NetworkScanner = new Windows.NetworkScanner();
                DriverScanner = new Windows.DriverScanner();
                ServiceScanner = new Windows.ServiceScanner();
                CommandHistoryScanner = new Windows.CommandHistoryScanner();
                ContainerScanner = null;
                break;
            case OS.MacOS:
                ProcessScanner = new MacOS.ProcessScanner();
                UserScanner = new MacOS.UserScanner();
                StorageScanner = new Common.GenericStorageScanner();
                PersistenceScanner = new MacOS.PersistenceScanner();
                NetworkScanner = new MacOS.NetworkScanner();
                DriverScanner = new MacOS.DriverScanner();
                ServiceScanner = new MacOS.ServiceScanner();
                CommandHistoryScanner = new Unix.CommandHistoryScanner();
                ContainerScanner = null;
                break;
            case OS.Linux:
                var ctx = new Linux.Context();

                ProcessScanner = new Linux.ProcessScanner(ctx);
                UserScanner = new Linux.UserScanner();
                StorageScanner = new Common.GenericStorageScanner();
                PersistenceScanner = new Linux.PersistenceScanner(ctx);
                NetworkScanner = new Linux.NetworkScanner(ctx);
                DriverScanner = new Linux.DriverScanner(ctx);
                ServiceScanner = null;
                CommandHistoryScanner = new Unix.CommandHistoryScanner();
                ContainerScanner = new Linux.ContainerScanner(ctx);
                break;
            case OS.FreeBSD:
                ProcessScanner = null;
                UserScanner = new Unix.UserScanner();
                StorageScanner = new Common.GenericStorageScanner();
                PersistenceScanner = new FreeBSD.PersistenceScanner();
                NetworkScanner = null;
                DriverScanner = new FreeBSD.DriverScanner();
                ServiceScanner = null;
                CommandHistoryScanner = new Unix.CommandHistoryScanner();
                ContainerScanner = null;
                break;
            default:
                throw new NotImplementedException($"{RuntimeInformation.OSDescription} is currently not supported");
        }
    }
}
