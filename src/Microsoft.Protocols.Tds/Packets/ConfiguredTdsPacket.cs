using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using System.Buffers;

namespace Microsoft.Protocols.Tds.Packets;

internal sealed class ConfiguredTdsPacket(TdsType type, WriterDelegate _writer, ReaderDelegate _reader, TdsConnectionDelegate _send) : ITdsPacket
{
    public TdsType Type => type;

    void ITdsPacket.Write(TdsConnectionContext context, IBufferWriter<byte> writer)
    {
        _writer(context, writer);
    }

    void ITdsPacket.Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
    {
        if (data.Length == 0)
        {
            return;
        }

        _reader(context, data);
    }

    public ValueTask SendAsync(TdsConnectionContext context) => _send(context);
}
