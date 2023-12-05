using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds.Packets;

public class TdsResponsePacket(TdsType type)
{
    public TdsType Type => type;

    public static TdsResponsePacket? Parse(ParsingContext context)
    {
        var feature = context.Context.Features.GetRequiredFeature<IPacketParserFeature>();
        var header = context.Read<TdsOptionsHeader>();
        var length = header.GetLength();

        var result = context.Input.Slice(0, length).Slice(Marshal.SizeOf<TdsOptionsHeader>());

        return context.ParseNext();
    }

}
