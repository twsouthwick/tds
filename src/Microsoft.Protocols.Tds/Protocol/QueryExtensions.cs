using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;

namespace Microsoft.Protocols.Tds;

public static class QueryExtensions
{
    public static void AddBatch(this IPacketCollectionBuilder builder)
    {
        builder.AddPacket(TdsType.SqlBatch, packet =>
        {
            packet.UseWrite((ctx, writer, next) =>
            {
                var query = ctx.Features.GetRequiredFeature<IQueryFeature>().QueryString;
                writer.WriteNullTerminated(query);

                next(ctx, writer);
            });
        });
    }
}
