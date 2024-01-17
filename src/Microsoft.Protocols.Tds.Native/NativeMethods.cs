using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.Protocols.Tds.Packets;

namespace Microsoft.Protocols.Tds.Native;

public class NativeMethods
{
    private static readonly ConcurrentDictionary<long, TdsConnection> _connections = new();

    [UnmanagedCallersOnly(EntryPoint = "tds_connection_open")]
    public static long OpenConnection(IntPtr str)
    {
        try
        {
            var connString = Marshal.PtrToStringUTF8(str);

            if (connString is null)
            {
                return -1;
            }

            var connection = new TdsConnection(_pipeline.Value, connString);
            var key = Random.Shared.NextInt64();

            while (!_connections.TryAdd(key, connection))
            {
                key = Random.Shared.NextInt64();
            }

            return key;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "tds_connection_close")]
    public static bool CloseConnection(long id)
    {
        return _connections.TryRemove(id, out _);
    }

    private static Lazy<TdsConnectionDelegate> _pipeline = new Lazy<TdsConnectionDelegate>(() =>
    {
        return TdsConnectionBuilder.Create()
            .UseSockets()
            .UseSqlAuthentication()
            .UseDefaultPacketProcessor()
            .UseAuthentication()
            .Build();
    }, isThreadSafe: true);
}
