namespace Pastarella.Terminal.Outputs;

public class TxtAppender
{
    private readonly List<string> _elements = [];

    public void Append(string? param, Presence presence = Presence.None)
    {
        if (string.IsNullOrEmpty(param)) return;

        string finalParam = presence switch
        {
            Presence.None => param,
            Presence.Required => $"[{param}]",
            Presence.Optional => $"({param})",
            _ => throw new ArgumentOutOfRangeException(nameof(presence), presence, null)
        };

        _elements.Add(finalParam);
    }

    public override string ToString()
    {
        return string.Join(" ", _elements);
    }
}

public enum Presence
{
    None,
    Required,
    Optional
}
