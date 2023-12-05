using Microsoft.Protocols.Tds.Packets;

namespace Microsoft.Protocols.Tds.Features;

public interface IPacketCollectionFeature
{
    ITdsPacket? Get(TdsType type);
}
