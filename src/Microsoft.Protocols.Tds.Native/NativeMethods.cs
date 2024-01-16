using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.Protocols.Tds.Packets;

namespace Microsoft.Protocols.Tds.Native;

public class NativeMethods
{
    private static readonly ConcurrentDictionary<long, Opened> _cache = new();

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

            var parser = new TdsConnection(_pipeline.Value, connString);
            var task = parser.ExecuteAsync();

            var opened = new Opened(parser, task.AsTask());
            var key = Random.Shared.NextInt64();

            while (!_cache.TryAdd(key, opened))
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

    [UnmanagedCallersOnly(EntryPoint = "tds_get_status")]
    public static int GetStatus(long id)
    {
        return _cache.TryGetValue(id, out var v) ? (int)v.task.Status : -1;
    }

    [UnmanagedCallersOnly(EntryPoint = "tds_connection_close")]
    public static bool CloseConnection(long id)
    {
        if (_cache.TryRemove(id, out var existing))
        {
            if (!existing.task.IsCompleted)
            {
                existing.conn.Context.Abort();
            }

            return true;
        }

        return false;
    }

    private record Opened(TdsConnection conn, Task task);

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
