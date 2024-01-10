using Microsoft.Protocols.Tds.Packets;

namespace Microsoft.Protocols.Tds.Features;

public interface IPacketFeature
{
    TdsType Type { get; set; }
}
