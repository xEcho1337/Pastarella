using Pastarella.Core.Models;

namespace Pastarella.Terminal.Outputs.Txt;

public class ForensicServices(OutputBuffer buffer)
{
    private readonly OutputBuffer Buffer = buffer;

    public void WriteProcesses(IEnumerable<ProcessInfo> processes)
    {
        Buffer.WriteLine("[PID] Name (Path) [Signed/Unsigned] - SHA256 - Start Time");

        foreach (var p in processes.OrderBy(p => p.Id))
        {
            Buffer.WriteLine($"[{p.Id}] {p.Name} ({p.Path}) [{p.Signer ?? "Unsigned"}] - {p.Sha256} - {p.StartTime}");
            TxtWriter.BasicPrintMetadata(Buffer, p.Metadata);
        }
    }

    public void WriteServices(IEnumerable<ServiceInfo> services)
    {
        Buffer.WriteLine("[Status] Service Name (Display Name) --> Path - SHA256");
        foreach (var service in services.OrderBy(p => p.ServiceName))
        {
            Buffer.WriteLine($"[{service.Status}] {service.ServiceName}");

            Buffer.Indent();
            Buffer.WriteLine($"|> Display: {service.DisplayName}");
            Buffer.WriteLine($"|> Command: {service.ExecPath} {string.Join(' ', service.Arguments)}");
            Buffer.WriteLine($"|> Hash: {service.Sha256}");
            Buffer.Unindent();
        }
    }

    public void WriteUsers(IEnumerable<UserInfo> users)
    {
        Buffer.WriteLine("[ID] User (disabled): description");
        foreach (var user in users.OrderBy(u => u.Uid))
        {
            Buffer.WriteLine($"[{user.Uid}] {user.Name}");

            Buffer.Indent();

            if (!string.IsNullOrWhiteSpace(user.Description))
                buffer.WriteLine($"|> Description: {user.Description}");

            buffer.WriteLine($"|> Home: {user.Home}");
            buffer.WriteLine($"|> Disabled: {user.Disabled}");
            TxtWriter.BasicPrintMetadata(Buffer, user.Metadata);

            Buffer.Unindent();
        }
    }

    public void WriteStorages(IEnumerable<StorageInfo> storages)
    {
        Buffer.WriteLine("Name (Type) -> Free/Total");

        foreach (var storage in storages)
        {
            long free = storage.FreeSpace / (1024 * 1024 * 1024);
            long total = storage.TotalSpace / (1024 * 1024 * 1024);

            Buffer.WriteLine($"{storage.Name} ({storage.Type}) -> {free} GB/{total} GB");
        }
    }
}

