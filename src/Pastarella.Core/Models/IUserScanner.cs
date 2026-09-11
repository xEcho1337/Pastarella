namespace Pastarella.Core.Models;

public record UserInfo(string Name, string Description, string Uid, string Home, bool Disabled)
{
    public Dictionary<string, object> Metadata { get; init; } = [];
}

public interface IUserScanner
{
    IEnumerable<UserInfo> Scan(IProgress<ScanProgress>? progress = null);
}

