using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Protocol;

namespace Microsoft.Protocols.Tds.Packets;

public static class PacketExtensions
{
    public static ITdsConnectionBuilder UseDefaultPacketProcessor(this ITdsConnectionBuilder builder)
        => builder.UsePacketProcessor(packet =>
        {
            packet.AddPreLogin();
            packet.AddLogin7();
            packet.AddBatch();
        });

    public static ITdsConnectionBuilder UsePacketProcessor(this ITdsConnectionBuilder builder, Action<IPacketCollectionBuilder> configure)
    {
        var packetBuilder = new PacketCollectionBuilder(builder.New());
        configure(packetBuilder);

        if (builder.Properties.TryGetValue(nameof(PacketCollectionBuilder), out var existing) && existing is IPacketCollectionBuilder existingBuilder)
        {
            configure(existingBuilder);
            return builder;
        }

        builder.Properties.Add(nameof(PacketCollectionBuilder), packetBuilder);

        return builder.Use((ctx, next) =>
        {
            ctx.Features.Set<IPacketCollectionFeature>(packetBuilder);
            return next(ctx);
        });
    }

    private class PacketCollectionBuilder(ITdsConnectionBuilder builder) : IPacketCollectionBuilder, IPacketCollectionFeature
    {
        private readonly Dictionary<TdsType, ITdsPacket> _lookup = new();

        public ITdsPacket? Get(TdsType type) => _lookup.TryGetValue(type, out var packet) ? packet : null;

        void IPacketCollectionBuilder.AddPacket(TdsType type, Action<IPacketBuilder> configure)
        {
            var options = new TdsPacketBuilder(type, builder.New());

            configure(options);

            _lookup.Add(type, options.Build());
        }
    }
}
