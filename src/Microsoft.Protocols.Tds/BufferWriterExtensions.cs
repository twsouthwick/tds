using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Protocols.Tds;

internal static class BufferWriterExtensions
{
    public static void WriteNullTerminated(this IBufferWriter<byte> writer, string str)
    {
#if NET
        Encoding.Unicode.GetBytes(str, writer);
        writer.Write((byte)0);
#else
        var bytes = Encoding.Unicode.GetBytes(str);
        writer.Write(bytes);
        writer.Write((byte)0);
#endif
    }

    public static void Write(this IBufferWriter<byte> writer, byte value)
    {
        var span = writer.GetSpan(1);

        span[0] = value;

        writer.Advance(1);
    }

    public static void WriteLittleEndian(this IBufferWriter<byte> writer, uint value)
    {
        var span = writer.GetSpan(4);
        BinaryPrimitives.WriteUInt32LittleEndian(span, value);
        writer.Advance(4);
    }

    public static void WriteBigEndian(this IBufferWriter<byte> writer, uint value)
    {
        var span = writer.GetSpan(4);
        BinaryPrimitives.WriteUInt32BigEndian(span, value);
        writer.Advance(4);
    }

    public static void Write<T>(this IBufferWriter<byte> writer, ref T value)
        where T : struct
    {
        var size = Marshal.SizeOf<T>();
        var span = writer.GetSpan(size);
#pragma warning disable CS9191 // The 'ref' modifier for an argument corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
        MemoryMarshal.Write(span, ref value);
#pragma warning restore CS9191 // The 'ref' modifier for an argument corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
        writer.Advance(size);
    }

    public static void Write(this IBufferWriter<byte> writer, bool value)
        => writer.Write((byte)(value ? 1 : 0));

    public static void WriteBigEndian(this IBufferWriter<byte> writer, int value)
        => writer.WriteBigEndian((uint)value);

    public static void WriteLittleEndian(this IBufferWriter<byte> writer, int value)
        => writer.WriteLittleEndian((uint)value);

    public static void Write(this IBufferWriter<byte> writer, Version version)
    {
        var span = writer.GetSpan(6);

        // Major and minor
        span[0] = (byte)(version.Major & 0xff);
        span[1] = (byte)(version.Minor & 0xff);

        // Build (Big Endian)
        span[2] = (byte)((version.Build & 0xff00) >> 8);
        span[3] = (byte)(version.Build & 0xff);

        // Sub-build (Little Endian)
        span[4] = (byte)(version.Revision & 0xff);
        span[5] = (byte)((version.Revision & 0xff00) >> 8);

        writer.Advance(6);
    }

    public static void Write(this IBufferWriter<byte> writer, Guid value)
    {
        var span = writer.GetSpan(16);

#if NET
        if (value.TryWriteBytes(span, bigEndian: true, out var written))
        {
            writer.Advance(written);
        }
        else
        {
            Debug.Assert(false, "Didn't write guid");
        }
#else
        writer.Write(value.ToByteArray());
#endif
    }

#if NETSTANDARD || NETFRAMEWORK
    public static long GetBytes(this Encoding encoding, string str, IBufferWriter<byte> writer)
    {
        var bytes = encoding.GetBytes(str);
        writer.Write(bytes);
        return bytes.Length;
    }
#endif
}
