using Microsoft.Protocols.Tds.Packets;

namespace Microsoft.Protocols.Tds.Features;

public interface ITdsConnectionFeature
{
    TdsType Type { get; }

    ValueTask WritePacket(ITdsPacket packet);

    ValueTask ReadPacketAsync(ITdsPacket packet);
}
