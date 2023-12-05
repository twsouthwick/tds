using System.Buffers;

namespace Microsoft.Protocols.Tds.Packets;

public interface IPacketProcessorBuilder
{
    void AddPacket(TdsType type, Action<IPacketBuilder> builder);
}

public interface IPacketBuilder
{
    void Add(byte optionKey, Action<TdsConnectionContext, IBufferWriter<byte>> func);
}
