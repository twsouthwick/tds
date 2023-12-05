using Microsoft.Protocols.Tds.Packets;

namespace Microsoft.Protocols.Tds.Features;

public interface ITdsConnectionFeature
{
    ValueTask WritePacket(ITdsPacket packet);

    ValueTask<ITdsPacket> ReadPacketAsync();
}
