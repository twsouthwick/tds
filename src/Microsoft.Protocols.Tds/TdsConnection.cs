using Microsoft.Protocols.Tds.Features;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;

namespace Microsoft.Protocols.Tds;

public sealed class TdsConnection : IConnectionStringFeature, IEnvironmentFeature
{
    private static readonly Version _version = typeof(TdsConnection).Assembly.GetName().Version ?? new Version(0, 0, 0, 0);

    private readonly TdsConnectionDelegate _tdsConnection;
    private readonly string _connectionString;

    public TdsConnection(TdsConnectionDelegate tdsConnection, string connectionString)
    {
        Context = new TdsConnectionContext();

        Context.Features.Set<IConnectionStringFeature>(this);
        Context.Features.Set<IEnvironmentFeature>(this);

        _tdsConnection = tdsConnection;
        _connectionString = connectionString;
    }

    public TdsConnectionContext Context { get; }

    public async ValueTask ExecuteAsync(string query)
    {
        await _tdsConnection(Context);
    }

    public bool TryGetValue(string key, out ReadOnlyMemory<char> value)
    {
        value = default;

#if NET8_0_OR_GREATER
        Span<char> keyWithEquals = stackalloc char[key.Length + 1];
        key.CopyTo(keyWithEquals);
#else
        var a = ArrayPool<char>.Shared.Rent(key.Length + 1);
        Span<char> keyWithEquals = a.AsSpan().Slice(0, key.Length + 1);
        key.CopyTo(0, a, 0, key.Length + 1);

        try
        {
#endif
        keyWithEquals[key.Length] = '=';

        var keyIndex = _connectionString.AsSpan().IndexOf(keyWithEquals, StringComparison.OrdinalIgnoreCase);

        if (keyIndex == -1)
        {
            return false;
        }

        value = _connectionString.AsMemory().Slice(keyIndex + key.Length + 1);

        var endIndex = value.Span.IndexOf(';');

        if (endIndex != -1)
        {
            value = value.Slice(0, endIndex);
        }

        return true;

#if !NET8_0_OR_GREATER
        }
        finally
        {
            ArrayPool<char>.Shared.Return(a);
        }
#endif
    }

    string IConnectionStringFeature.ConnectionString => _connectionString;

    Version IEnvironmentFeature.Version => _version;

    int IEnvironmentFeature.ThreadId => Environment.CurrentManagedThreadId;

    string IEnvironmentFeature.HostName => Environment.MachineName;

    string IEnvironmentFeature.AppName { get; } = Assembly.GetExecutingAssembly().FullName ?? string.Empty;

    string IEnvironmentFeature.ServerName { get; } = "Server";

    ReadOnlySpan<byte> IEnvironmentFeature.ClientId => GetClientId.Value.AsSpan(0, 6);

    string IEnvironmentFeature.LibraryName => "Microsoft.Protocols.Tds";

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
