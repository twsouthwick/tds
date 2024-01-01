using Microsoft.Extensions.ObjectPool;
using Microsoft.Protocols.Tds;
using System.Buffers;

namespace Microsoft.Protocols.Tds.Packets;

internal sealed class ConfiguredTdsPacket(TdsType type, ObjectPool<ArrayBufferWriter<byte>> pool, WriterDelegate _writer, ReaderDelegate _reader) : ITdsPacket
{
    public TdsType Type => type;

    void ITdsPacket.Write(TdsConnectionContext context, IBufferWriter<byte> writer)
    {
        var payload = pool.Get();

        try
        {
            _writer(context, payload);

            writer.WriteHeader(Type, (short)payload.WrittenCount);

            writer.Write(payload.WrittenSpan);
        }
        finally
        {
            pool.Return(payload);
        }
    }

    void ITdsPacket.Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
    {
        if (data.Length == 0)
        {
            return;
        }

        _reader(context, data);
    }
}
