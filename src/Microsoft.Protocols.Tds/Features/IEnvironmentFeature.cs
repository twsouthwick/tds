namespace Microsoft.Protocols.Tds.Features;

public interface IEnvironmentFeature
{
    Version Version { get; }

    int ThreadId { get; }

    int ProcessId { get; }

    string HostName { get; }

    string AppName { get; }

    string ServerName { get; }

    ReadOnlySpan<byte> ClientId { get; }
}

