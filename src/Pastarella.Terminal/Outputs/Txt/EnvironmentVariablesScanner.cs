namespace Pastarella.Terminal.Outputs.Txt;

public class EnvironmentVariablesScanner(OutputBuffer buffer)
{
    private readonly OutputBuffer Buffer = buffer;

    public void WriteEnvs(Dictionary<string, string> envs)
    {
        foreach (var (key, val) in envs)
            Buffer.WriteLine($"{key} = {val}");
    }
}

