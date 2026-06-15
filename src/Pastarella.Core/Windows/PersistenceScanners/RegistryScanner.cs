using Microsoft.Win32;
using Pastarella.Core.Models;

namespace Pastarella.Core.Windows.PersistenceScanners;

public class RegistryScanner : IPersistenceScanner
{
    private enum RootKey
    {
        LocalMachine,
        User,
    }

    private record ToCheck(
        RootKey[] RootKeys,
        string SubkeyPath,
        (string /* key name */, object? /* key expected value */)[]? Values,
        bool ShouldExist = true // only used when `Values` is null
    );

    private static readonly ToCheck[] KeysToCheck = [
        new([RootKey.LocalMachine], @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", [("Svchost", null)]),

        new(
            [RootKey.LocalMachine, RootKey.User], @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon",
            [
                ("Notify", null),
                ("Userinit", @"C:\WINDOWS\system32\userinit.exe,"),
                ("Shell", "explorer.exe"),
                ("ShellAppRuntime", "ShellAppRuntime.exe"),
            ]
        ),

        new(
            [RootKey.User], @"SOFTWARE\Microsoft\Windows NT\Windows",
            [
                ("Load", null),
                ("Run", null),
            ]
        ),

        new([RootKey.LocalMachine, RootKey.User], @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunServices", null),
        new([RootKey.LocalMachine, RootKey.User], @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunServicesOnce", null),

        new([RootKey.LocalMachine, RootKey.User], @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", null),
        new([RootKey.LocalMachine, RootKey.User], @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", null),

        new([RootKey.LocalMachine], @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", null),
        new([RootKey.LocalMachine], @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce", null),

        new([RootKey.LocalMachine], @"SOFTWARE\Microsoft\NetSh", null),

        new(
            [RootKey.LocalMachine], @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows",
            [
                ("AppInit_DLLs", ""),
                ("LoadAppInit_DLLs", 0),
                ("RequireSignedAppInit_DLLs", null),
            ]
        ),

        new(
            [RootKey.LocalMachine], @"SYSTEM\CurrentControlSet\Control\Session Manager",
            [
                ("BootExecute", "autocheck autochk *"),
                ("BootShell", "%SystemRoot%\\system32\\bootim.exe"),
            ]
        ),

        new([RootKey.LocalMachine, RootKey.User], @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run", null, false),

        new([RootKey.LocalMachine], @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", null),

        // https://attack.mitre.org/techniques/T1037/001/
        new([RootKey.User], "Environment", [("UserInitMprLogonScript", null)]),

        // Not documented and deprecated
        new([RootKey.LocalMachine], @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnceEx", null, false),

        // I do not think that anyone uses this
        new([RootKey.User], @"SOFTWARE\Control Panel\Desktop", [("SCRNSAVE.EXE", null)]),
    ];

    private static bool OfflineRegistryExist(out string path)
    {
        path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return File.Exists(Path.Combine(path, "NTUSER.MAN"));
    }

    private static string KindLikeRegedit(RegistryValueKind kind)
        => kind switch
        {
            RegistryValueKind.None => "REG_NONE",
            RegistryValueKind.Unknown => "unknown",
            RegistryValueKind.String => "REG_SZ",
            RegistryValueKind.ExpandString => "REG_EXPAND_SZ",
            RegistryValueKind.Binary => "REG_BINARY",
            RegistryValueKind.DWord => "REG_DWORD",
            RegistryValueKind.MultiString => "REG_MULTI_SZ",
            RegistryValueKind.QWord => "REG_QWORD",

            _ => throw new NotImplementedException(),
        };

    private static List<(string Key, RegistryValueKind Kind, object? Value)> ReadRegistryValues(RegistryKey root, string subKeyPath)
    {
        List<(string, RegistryValueKind, object?)> list = [];

        try
        {
            using var keys = root.OpenSubKey(subKeyPath);

            if (keys == null)
                return list;

            foreach (string name in keys.GetValueNames())
                list.Add((keys.Name, keys.GetValueKind(name), keys.GetValue(name)));
        }
        catch
        {
            // ignored
        }

        return list;
    }

    private static (RegistryValueKind Kind, object? Value)? ReadRegistryValue(RegistryKey root, string subKeyPath, string key)
    {
        try
        {
            using var keys = root.OpenSubKey(subKeyPath);

            if (keys == null)
                return null;

            object? val = keys.GetValue(key);
            return val == null ? null
                : (keys.GetValueKind(key), val);
        }
        catch
        {
            return null;
        }
    }

    private static int GetRiskScore(RegistryValueKind kind, object? value, object? checkerValue, bool shouldExist)
    {
        if (checkerValue == null)
            return 0;

        if (!shouldExist && kind != RegistryValueKind.None)
            return 25;

        switch (kind)
        {
            case RegistryValueKind.None:
                return 0;

            case RegistryValueKind.Binary:
                return value == checkerValue ? 0 : 10;

            case RegistryValueKind.String:
            case RegistryValueKind.ExpandString:
            case RegistryValueKind.MultiString:
                string val = value is string[] multiline_val ? string.Join('\n', multiline_val) : (string)value!;
                string _checkerVal = checkerValue is string[] multiline_checker ? string.Join('\n', multiline_checker) : (string)checkerValue!;
                string checkerVal = PlatformHelpers.NormalizePath(_checkerVal) ?? _checkerVal;

                if (val == checkerVal)
                    return 0;

                if (val.Contains(checkerVal))
                    return 20;
                else
                    return 10;

            case RegistryValueKind.DWord:
                return (int)value! == (int)checkerValue! ? 0 : 10;

            case RegistryValueKind.QWord:
                return (long)value! == (long)checkerValue! ? 0 : 10;

            default:
                throw new NotImplementedException();
        }
    }

    private static void ScanSubkey(
        ref List<PersistenceEntry> list,
        PersistencePrivilege privilege,
        RegistryKey root,
        string path,
        (string, object?)[]? values,
        bool shouldExist
    )
    {
        if (values != null)
        {
            foreach (var (key, checkValue) in values)
            {
                var entry = ReadRegistryValue(root, path, key);
                if (entry != null)
                {
                    string? filePath = entry.Value.Value?.ToString();
                    int riskScore = GetRiskScore(entry.Value.Kind, entry.Value.Value, checkValue, shouldExist);

                    if (riskScore != 0)
                    {
                        list.Add(new PersistenceEntry
                        {
                            Name = key,
                            Path = path,
                            Action = new ExecScheduledAction
                            {
                                Path = filePath ?? "",
                                Sha256 = PlatformHelpers.GetSha256(filePath),
                            },

                            Privilege = privilege,
                            Type = PersistenceType.RegistryKey,
                            RiskScore = riskScore,

                            Metadata = new Dictionary<string, object>
                            {
                                ["ValueKind"] = KindLikeRegedit(entry.Value.Kind),
                            }
                        });
                    }
                }
            }
        }
        else
        {
            foreach (var (key, kind, val) in ReadRegistryValues(root, path))
            {
                string? filePath = val?.ToString();
                list.Add(new PersistenceEntry
                {
                    Name = key,
                    Path = path,
                    Action = new ExecScheduledAction
                    {
                        Path = filePath ?? "",
                        Sha256 = PlatformHelpers.GetSha256(filePath),
                    },

                    Privilege = privilege,
                    Type = PersistenceType.RegistryKey,
                    RiskScore = shouldExist ? 0 : 25,

                    Metadata = new Dictionary<string, object>
                    {
                        ["ValueKind"] = KindLikeRegedit(kind),
                    }
                });
            }
        }
    }

    public IEnumerable<PersistenceEntry> Scan()
    {
        List<PersistenceEntry> list = [];
        IEnumerable<string /* SID */> user_sid_cache = ForensicScanner.CachedUserInfo.Select(u => u.Item2);

        foreach (var toCheck in KeysToCheck)
        {
            foreach (var root in toCheck.RootKeys)
            {
                switch (root)
                {
                    case RootKey.LocalMachine:
                        ScanSubkey(ref list, PersistencePrivilege.Admin, Registry.LocalMachine, toCheck.SubkeyPath, toCheck.Values, toCheck.ShouldExist);
                        break;
                    case RootKey.User:
                        foreach (string sid in user_sid_cache)
                            ScanSubkey(ref list, PersistencePrivilege.User, Registry.Users.OpenSubKey(sid), toCheck.SubkeyPath, toCheck.Values, toCheck.ShouldExist);
                        break;
                }
            }
        }

        if (OfflineRegistryExist(out string regPath))
        {
            list.Add(new PersistenceEntry
            {
                Name = "NTUSER.MAN",
                Path = regPath,
                Action = new ExecScheduledAction
                {
                    Path = "" // TODO:
                },

                Privilege = PersistencePrivilege.User,
                Type = PersistenceType.OfflineRegistry,
            });
        }

        return list;
    }
}

