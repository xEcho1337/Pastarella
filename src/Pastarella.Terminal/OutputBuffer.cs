namespace Pastarella.Terminal;

public class OutputBuffer
{
    public string Text { get; private set; } = "";
    private int _indent = 0;

    public void Write(char text) => Text += text;

    public void Write(string text) => Text += text;

    public void WriteLine(string text)
    {
        string prefix = string.Concat(Enumerable.Repeat("  ", _indent * 2));
        Write(prefix + text + Environment.NewLine);
    }

    public void Indent()
    {
        _indent++;
    }

    public void Unindent()
    {
        _indent--;
    }
}
