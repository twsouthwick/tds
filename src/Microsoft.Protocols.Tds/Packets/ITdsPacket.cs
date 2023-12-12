using System.Buffers;

namespace Microsoft.Protocols.Tds.Packets;

public interface ITdsPacket
{
    TdsType Type { get; }

    void Write(TdsConnectionContext context, IBufferWriter<byte> writer);

    void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data);
}
