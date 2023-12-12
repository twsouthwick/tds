using Microsoft.Protocols.Tds.Features;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Microsoft.Protocols.Tds.Protocol;

internal ref struct OffsetWriter(Span<byte> offset, ArrayBufferWriter<byte> payload)
{
    private readonly Span<byte> offset = offset;
    private int count = 0;

    public void Add(string name)
    {
        BinaryPrimitives.WriteInt16LittleEndian(offset.Slice(4 * count, 2), (short)payload.WrittenCount);
        var length = Encoding.Unicode.GetBytes(name, payload);
        BinaryPrimitives.WriteInt16LittleEndian(offset.Slice(4 * count + 2, 2), (short)length);
        count++;
    }

    public void Add(ISspiAuthenticationFeature sspi)
    {
        BinaryPrimitives.WriteInt16LittleEndian(offset.Slice(4 * count, 2), (short)payload.WrittenCount);
        var length = payload.WrittenCount;
        sspi.WriteBlock(Array.Empty<byte>(), payload);
        BinaryPrimitives.WriteInt16LittleEndian(offset.Slice(4 * count + 2, 2), (short)(payload.WrittenCount - length));
        count++;
    }

    public readonly void AddOffset(short offset)
    {
        for (int i = 0; i < count; i += 4)
        {
            var slice = this.offset.Slice(i, 2);
            var current = BinaryPrimitives.ReadInt16LittleEndian(slice);
            BinaryPrimitives.WriteInt16LittleEndian(slice, (short)(current + offset));
        }
    }

    public static OffsetWriter Create(int count, IBufferWriter<byte> writer, ArrayBufferWriter<byte> payload)
    {
        var hint = count * 4;
        var span = writer.GetSpan(hint);
        writer.Advance(hint);

        return new OffsetWriter(span, payload);
    }
}
