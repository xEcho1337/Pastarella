using Pastarella.Core.Models;

namespace Pastarella.Terminal.Outputs.Txt;

public class ForensicServices(OutputBuffer buffer)
{
    public void WriteProcesses(IEnumerable<ProcessInfo> processes)
    {
        buffer.WriteLine("[Start Time] [PID] [Path] (Command Args) [Signed/Unsigned] (SHA256)");

        foreach (var p in processes.OrderBy(p => p.Id))
        {
            bool signed = p.ExePath.Signature != null;

            var outBuf = new TxtAppender();

            outBuf.Append(p.StartTime?.ToString(), Presence.Required);
            outBuf.Append($"{p.Id}", Presence.Required);
            outBuf.Append(p.ExePath.NormalizedValue, Presence.Required);
            outBuf.Append(p.CommandArgs == null ? "" : string.Join(' ', p.CommandArgs), Presence.Optional);
            outBuf.Append(signed ? "Signed" : "Unsigned", Presence.Required);
            outBuf.Append(p.ExePath.Sha256, Presence.Optional);

            buffer.WriteLine(outBuf.ToString());
            TxtWriter.BasicPrintMetadata(buffer, p.Metadata);
        }
    }

    public void WriteServices(IEnumerable<ServiceInfo> services)
    {
        buffer.WriteLine("[Status] [Service Name] (Display Name) [Type] [Path] (Arguments) [Signed/Unsigned] (SHA256)");
        foreach (var service in services.OrderBy(p => p.ServiceName))
        {
            bool signed = service.ExePath?.Signature != null;

            var outBuf = new TxtAppender();

            outBuf.Append(service.ServiceType.ToString(), Presence.Required);
            outBuf.Append(service.Status.ToString(), Presence.Required);
            outBuf.Append(service.ServiceName, Presence.Required);

            outBuf.Append(service.ExePath?.NormalizedValue, Presence.Required);
            outBuf.Append(string.Join(' ', service.Arguments), Presence.Optional);
            outBuf.Append(signed ? "Signed" : "Unsigned", Presence.Required);
            outBuf.Append(service.ExePath?.Sha256, Presence.Optional);

            buffer.WriteLine(outBuf.ToString());
            TxtWriter.PrintExePath(buffer, service.ExePath);
        }
    }

    public void WriteUsers(IEnumerable<UserInfo> users)
    {
        buffer.WriteLine("[UID] [Name] (Description) [Home] [Enabled/Disabled]");
        foreach (var user in users.OrderBy(u => u.Uid))
        {
            var outBuf = new TxtAppender();

            outBuf.Append(user.Uid, Presence.Required);
            outBuf.Append(user.Name, Presence.Required);
            outBuf.Append(user.Description, Presence.Optional);
            outBuf.Append(user.Home, Presence.Required);
            outBuf.Append(user.Disabled ? "Disabled" : "Enabled", Presence.Required);

            buffer.WriteLine(outBuf.ToString());
            TxtWriter.BasicPrintMetadata(buffer, user.Metadata);
        }
    }

    public void WriteStorages(IEnumerable<StorageInfo> storages)
    {
        buffer.WriteLine("[Name] [Type] [Free/Total]");

        foreach (var storage in storages)
        {
            ulong free = storage.FreeSpace / (1024 * 1024 * 1024);
            ulong total = storage.TotalSpace / (1024 * 1024 * 1024);

            var outBuf = new TxtAppender();

            outBuf.Append(storage.Name, Presence.Required);
            outBuf.Append(storage.Type.ToString(), Presence.Required);
            outBuf.Append($"{free} GB/{total} GB", Presence.Required);

            buffer.WriteLine(outBuf.ToString());
        }
    }
}
