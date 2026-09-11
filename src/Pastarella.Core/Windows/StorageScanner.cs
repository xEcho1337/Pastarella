using System.Diagnostics;
using Pastarella.Core.Models;

using static Vanara.PInvoke.Kernel32;

namespace Pastarella.Core.Windows;

public class StorageScanner : IStorageScanner
{
    private static List<StorageInfo>? CachedStorageInfo;
    public static Dictionary<char, string> DriveMap
    {
        get
        {
            if (field == null)
            {
                field = [];

                for (char c = 'A'; c <= 'Z'; c++)
                {
                    var devicePath = QueryDosDevice($"{c}:");
                    if (!devicePath.Any())
                        continue;

                    field.Add(c, devicePath.First());
                }
            }

            return field;
        }
    }

    private static DriveType ConvertFromVanara(DRIVE_TYPE type)
        => type switch
        {
            DRIVE_TYPE.DRIVE_UNKNOWN => DriveType.Unknown,
            DRIVE_TYPE.DRIVE_NO_ROOT_DIR => DriveType.NoRootDirectory,
            DRIVE_TYPE.DRIVE_REMOVABLE => DriveType.Removable,
            DRIVE_TYPE.DRIVE_FIXED => DriveType.Fixed,
            DRIVE_TYPE.DRIVE_REMOTE => DriveType.Network,
            DRIVE_TYPE.DRIVE_CDROM => DriveType.CDRom,
            DRIVE_TYPE.DRIVE_RAMDISK => DriveType.Ram,
            _ => throw new UnreachableException(),
        };

    private static List<StorageInfo> ScanDrives()
    {
        var list = new List<StorageInfo>();

        foreach ((char letter, string _) in DriveMap)
        {
            string deviceName = $"{letter}:";

            var type = ConvertFromVanara(GetDriveType(deviceName));

            if (!GetDiskFreeSpaceEx(deviceName, out ulong _, out ulong totalNumberOfBytes, out ulong totalNumberOfFreeBytes))
                list.Add(new(type, deviceName, 0, 0));
            else
                list.Add(new(type, deviceName, totalNumberOfFreeBytes, totalNumberOfBytes));
        }

        return list;
    }

    public IEnumerable<StorageInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        return CachedStorageInfo ??= ScanDrives();
    }
}

