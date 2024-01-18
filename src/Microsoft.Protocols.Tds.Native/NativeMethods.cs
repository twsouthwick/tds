using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds.Native;

public class NativeMethods
{
    private static readonly ConcurrentDictionary<long, DefaultTdsConnectionContext> _connections = new();

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

            var key = Random.Shared.NextInt64();

#if FALSE
            var connection = new DefaultTdsConnectionContext(_pipeline.Value, connString);

            while (!_connections.TryAdd(key, connection))
            {
                key = Random.Shared.NextInt64();
            }
#endif

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
}
