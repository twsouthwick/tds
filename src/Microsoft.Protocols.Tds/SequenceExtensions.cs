using System.Buffers;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds;

internal static class SequenceExtensions
{
    public static T Read<T>(this ReadOnlySequence<byte> data)
        where T : struct
    {
        if (data.First.Span.Length < Marshal.SizeOf<T>())
        {
            throw new InvalidOperationException("Need to move to span");
        }

        return MemoryMarshal.Read<T>(data.First.Span);
    }

    public static bool ReadBoolean(this ReadOnlySequence<byte> data)
        => data.ReadByte() == 1;

    public static byte ReadByte(this ReadOnlySequence<byte> data)
        => data.First.Span[0];
}
