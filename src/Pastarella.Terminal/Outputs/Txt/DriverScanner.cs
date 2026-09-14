using Pastarella.Core.Models;

namespace Pastarella.Terminal.Outputs.Txt;

public class DriverScanner(OutputBuffer buffer)
{
    private readonly OutputBuffer Buffer = buffer;

    public void WriteDrivers(IEnumerable<DriverInfo> drivers)
    {
        Buffer.WriteLine("[Type] (Loaded) Identifier -> ExecutablePath [SHA256]");
        foreach (var driver in drivers.OrderBy(d => d.Name))
        {
            string loaded = driver.Loaded ? "Loaded" : "Not Loaded";
            string version = driver.Version ?? "";

            Buffer.WriteLine($"[{driver.Type}] {driver.Identifier}/{driver.DisplayName}");
            Buffer.WriteLine($"  > State: {loaded}");
            Buffer.WriteLine($"  > Version: {version}");
            TxtWriter.PrintExePath(buffer, driver.ExePath);

            Buffer.WriteLine("");
        }
    }
}
