using Microsoft.Extensions.Logging;
using Microsoft.Protocols.Tds.Features;

namespace Microsoft.Protocols.Tds.Packets;

internal sealed class DefaultParserFeature(ILogger<DefaultParserFeature> logger) : IPacketParserFeature
{
    public TdsResponsePacket? Parse(TdsType type, ParsingContext context) => type switch
    {
        TdsType.Table => TableTdsResponsePacket.Parse(context),
        _ => GetUnknown(type),
    };

    private TdsResponsePacket? GetUnknown(TdsType type)
    {
        logger.LogWarning("Unknown TDS packet '{Type}'", (byte)type);
        return null;
    }

    public TdsResponsePacket? Parse(ParsingContext context)
        => Parse((TdsType)context.Input.First.Span[0], context);
}

