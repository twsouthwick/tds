using Microsoft.Extensions.ObjectPool;
using System.Buffers;

namespace Microsoft.Protocols.Tds.Packets;

public interface IPacketBuilder
{
    IServiceProvider Services { get; }

    IPacketBuilder AddWriter(WriterDelegate writer);

    IPacketBuilder AddHandler(Action<ITdsConnectionBuilder> builder);

    ITdsPacket Build();
}
