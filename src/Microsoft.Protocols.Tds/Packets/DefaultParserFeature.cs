using Microsoft.Protocols.Tds.Features;

namespace Microsoft.Protocols.Tds.Packets;

internal sealed class DefaultParserFeature : IPacketParserFeature
{
    public TdsResponsePacket? Parse(TdsType type, ParsingContext context) => type switch
    {
        TdsType.Table => TableTdsResponsePacket.Parse(context),
        _ => null,
    };

    public TdsResponsePacket? Parse(ParsingContext context)
        => Parse((TdsType)context.Input.First.Span[0], context);
}

