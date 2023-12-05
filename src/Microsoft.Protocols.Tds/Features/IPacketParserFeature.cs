using Microsoft.Protocols.Tds.Packets;

namespace Microsoft.Protocols.Tds.Features;

public interface IPacketParserFeature
{
    TdsResponsePacket? Parse(ParsingContext context);
}
