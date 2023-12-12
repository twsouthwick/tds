using Microsoft.Protocols.Tds.Features;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Microsoft.Protocols.Tds.Protocol;

internal ref struct OffsetWriter(Span<byte> offset, IBufferWriter<byte> writer, ArrayBufferWriter<byte> payload)
{
    private int offsetLength = offset.Length;
    private Span<byte> _currentBuffer = offset;

    private readonly void WriteOffset()
    {
        var o = (short)(offsetLength + payload.WrittenCount);
        BinaryPrimitives.WriteInt16LittleEndian(_currentBuffer.Slice(0, 2), o);
    }

    private readonly void WriteLength(long length)
    {
        BinaryPrimitives.WriteInt16LittleEndian(_currentBuffer.Slice(2, 2), (short)length);
    }

    public void AddPayload(ReadOnlySpan<byte> items)
    {
        WriteOffset();
        payload.Write(items);
        WriteLength(items.Length);
        Advance();
    }

    private void Advance()
    {
        _currentBuffer = _currentBuffer.Slice(4);
    }

    public void WritePayload()
    {
        WriteOffset();
        WriteLength(0);
        Advance();
    }

    public void WritePayload(string name)
    {
        WriteOffset();
        var length = Encoding.Unicode.GetBytes(name, payload);
        WriteLength(length);
        Advance();
    }

    public void WritePayload(ISspiAuthenticationFeature sspi)
    {
        WriteOffset();
        var length = payload.WrittenCount;
        sspi.WriteBlock(Array.Empty<byte>(), payload);
        WriteLength(payload.WrittenCount - length);
        Advance();
    }

    public void WriteOffset(ReadOnlySpan<byte> bytes)
    {
        bytes.CopyTo(_currentBuffer);
        _currentBuffer = _currentBuffer.Slice(bytes.Length);
    }

    public void Complete()
    {
        writer.Write(payload.WrittenSpan);
    }

    public static OffsetWriter Create(int count, IBufferWriter<byte> writer, ArrayBufferWriter<byte> payload, int additionalCount = 0)
    {
        var hint = count * 4 + additionalCount;
        var span = writer.GetSpan(hint).Slice(0, hint);
        writer.Advance(hint);

        return new OffsetWriter(span, writer, payload);
    }
}
