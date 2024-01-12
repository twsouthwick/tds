using Microsoft.Protocols.Tds.Features;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;

namespace Microsoft.Protocols.Tds;

public sealed class TdsParser(TdsConnectionDelegate tdsConnection) : IConnectionStringFeature, IEnvironmentFeature, ISqlUserAuthenticationFeature
{
    private static readonly Version _version = typeof(TdsParser).Assembly.GetName().Version ?? new Version(0, 0, 0, 0);

    public async ValueTask ExecuteAsync()
    {
        var context = new TdsConnectionContext();

        context.Features.Set<IConnectionStringFeature>(this);
        context.Features.Set<IEnvironmentFeature>(this);
        context.Features.Set<ISqlUserAuthenticationFeature>(this);

        await tdsConnection(context);
    }

    string IConnectionStringFeature.ConnectionString => string.Empty;

    public required EndPoint Endpoint { get; init; }

    public required string Database { get; init; }

    public Version Version => _version;

    public int ThreadId => Environment.CurrentManagedThreadId;

    public string HostName => Environment.MachineName;

    public required string AppName { get; init; }

    public required string ServerName { get; init; }

    public ReadOnlySpan<byte> ClientId => GetClientId.Value.AsSpan(0, 6);

    public required string UserName { get; init; }

    public required string Password { get; init; }

    public string LibraryName { get; } = "Core Microsoft SqlClient Data Provider";

    int IEnvironmentFeature.ProcessId

#if NET8_0_OR_GREATER
        => Environment.ProcessId;

#else
        => _processId.Value;

    private static Lazy<int> _processId = new Lazy<int>(() =>
    {
        using var p = Process.GetCurrentProcess();
        return p.Id;
    }, isThreadSafe: true);
#endif


    private static Lazy<byte[]> GetClientId = new Lazy<byte[]>(() =>
    {
        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus == OperationalStatus.Up)
            {
                return nic.GetPhysicalAddress().GetAddressBytes();
            }
        }

        return new byte[6];

    }, isThreadSafe: true);
}
