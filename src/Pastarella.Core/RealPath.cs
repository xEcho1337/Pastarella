using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace Pastarella.Core;

public abstract class RealPath(string path, bool normalized = false)
{
    // If you explicitly want the normalized path use `NormalizedValue`.
    [JsonIgnore]
    public string Value { get; private set; } = path;
    private bool normalized = normalized;

    // If we explicitly want the normalized path.
    public virtual string NormalizedValue
    {
        get
        {
            if (!normalized)
            {
                normalized = true;
                Value = PathConverter.Normalize(Value);
            }
            return Value;
        }
    }

    public bool Exist()
    {
        return File.Exists(NormalizedValue);
    }
}

public class ExePath(
    string path,
    bool normalized = false,
    ISignature? signature = null,
    bool isRealPath = true
) : RealPath(path, normalized)
{
    public string? Sha256 => (isRealPath && Exist()) ? PlatformHelpers.GetSha256(NormalizedValue) : null;
    public ISignature? Signature => signature ??= (isRealPath && Exist()) ? GetSignature(NormalizedValue) : null;

    private static ISignature? GetSignature(string path)
    {
        switch (Context.Os)
        {
            case Context.OS.Windows:
                try
                {
                    return new WindowsSignature(X509Certificate.CreateFromSignedFile(path));
                }
                catch (Exception)
                {
                    // ignore
                }
                break;
            default:
                break;
        }
        return null;
    }

    public static ExePath? FromCmdline(string cmdline, out string[] args)
    {
        string[] parts = PathConverter.Unescape(cmdline);

        args = (parts.Length > 1) ? parts[1..] : [];

        if (parts.Length == 0)
            return null;

        return new(parts[0]);
    }
}

public class FakeExePath(string path) : ExePath(path, false, isRealPath: false)
{
    public override string NormalizedValue => Value;
}
