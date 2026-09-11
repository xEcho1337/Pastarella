namespace Pastarella.Core.Models;

public record ScanProgress(
    int Done,
    int? Total = null,
    string? Phase = null
);
