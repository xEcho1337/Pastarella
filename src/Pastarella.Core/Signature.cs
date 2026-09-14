using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace Pastarella.Core;

// TODO: When C#15 is released use a union: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15#union-types

[JsonDerivedType(typeof(MacOSSignature))]
[JsonDerivedType(typeof(WindowsSignature))]
public interface ISignature;

public class WindowsSignature(X509Certificate certificate) : ISignature
{
    public readonly string Issuer = certificate.Issuer;
    public readonly string Subject = certificate.Subject;
}

public record MacOSSignature(string? TeamIdentifier, string? Autority) : ISignature;
