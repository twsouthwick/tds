using Microsoft.Protocols.Tds.Features;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Protocols.Tds.Protocol;

internal struct OffsetWriter(int length, int initialOffset, IBufferWriter<byte> writer, ArrayBufferWriter<byte> payload)
{
    private int count = 0;

    private readonly void WriteOffset(Span<byte> buffer)
    {
        var o = (short)(initialOffset + length + payload.WrittenCount);
        BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(0, 2), o);
    }

    private readonly void WriteLength(Span<byte> buffer, long length)
    {
        BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(2, 2), (short)length);
    }

    private void Advance()
    {
        count += 4;
        writer.Advance(4);
    }

    public void WritePayload(ReadOnlySpan<byte> items)
    {
        var buffer = writer.GetSpan(4);
        WriteOffset(buffer);
        payload.Write(items);
        WriteLength(buffer, items.Length);
        Advance();
    }

    public void WritePayload()
    {
        var buffer = writer.GetSpan(4).Slice(0, 4);
        buffer.Clear();
        Advance();
    }

    public void WritePayload(string name, bool encrypt = false)
    {
        var buffer = writer.GetSpan(4);
        WriteOffset(buffer);

        if (encrypt)
        {
            var span = payload.GetSpan(name.Length * 2);
            var asSpan = MemoryMarshal.AsBytes(name.AsSpan());

            for (int i = 0; i < asSpan.Length; i++)
            {
                span[i] = Swap(asSpan[i]);
            }

            static byte Swap(byte b) => (byte)((((b & 0x0f) << 4) | (b >> 4)) ^ 0xa5);

            payload.Advance(name.Length * 2);
        }
        else
        {
            Encoding.Unicode.GetBytes(name, payload);
        }

        WriteLength(buffer, name.Length);
        Advance();
    }

    public void WritePayload(ISspiAuthenticationFeature sspi)
    {
        var buffer = writer.GetSpan(4);
        WriteOffset(buffer);
        var length = payload.WrittenCount;
        sspi.WriteBlock(Array.Empty<byte>(), payload);
        WriteLength(buffer, payload.WrittenCount - length);
        Advance();
    }

    public void WriteOffset(ReadOnlySpan<byte> bytes)
    {
        writer.Write(bytes);
        count += bytes.Length;
    }

    public void Complete()
    {
        if (count != length)
        {
            throw new InvalidOperationException("Count was not as expected");
        }

        writer.Write(payload.WrittenSpan);
    }

    public static OffsetWriter Create(int count, ArrayBufferWriter<byte> writer, ArrayBufferWriter<byte> payload, int additionalCount = 0)
        => Create(count, writer.WrittenCount, writer, payload, additionalCount);

    public static OffsetWriter Create(int count, int initialOffset, IBufferWriter<byte> writer, ArrayBufferWriter<byte> payload, int additionalCount = 0)
    {
        var max = count * 4 + additionalCount;

        return new OffsetWriter(max, initialOffset, writer, payload);
    }
}
