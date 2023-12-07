using System.Buffers;

namespace Microsoft.Protocols.Tds.Packets;

public interface IPacketCollectionBuilder
{
    IPacketOptionBuilder AddPacket(TdsType type);
}

public interface IPacketOptionBuilder
{
    IPacketOptionBuilder AddOption(IPacketOption option);

    IPacketOptionBuilder AddHandler(Action<ITdsConnectionBuilder> builder);
}

public interface IPacketOption
{
    void Write(TdsConnectionContext context, IBufferWriter<byte> writer);

    void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data);
}
