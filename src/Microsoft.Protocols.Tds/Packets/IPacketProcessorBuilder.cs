using System.Buffers;

namespace Microsoft.Protocols.Tds.Packets;

public interface IPacketProcessorBuilder
{
    void AddPacket(TdsType type, Action<IPacketOptionBuilder> builder);
}

public interface IPacketOptionBuilder
{
    void Add(IPacketOption option);
}

public interface IPacketOption
{
    void Write(TdsConnectionContext context, IBufferWriter<byte> writer);

    void Read(TdsConnectionContext context, ReadOnlySpan<byte> data);
}
