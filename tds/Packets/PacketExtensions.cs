using Microsoft.Protocols.Tds.Features;

namespace Microsoft.Protocols.Tds.Packets;

public static class PacketExtensions
{
    public static ITdsConnectionBuilder UseDefaultPacketProcessor(this ITdsConnectionBuilder builder)
        => builder.UsePacketProcessor(packet =>
        {
            packet.AddPreLogin();
        });

    public static ITdsConnectionBuilder UsePacketProcessor(this ITdsConnectionBuilder builder, Action<IPacketProcessorBuilder> configure)
    {
        var packetBuilder = new PacketBuilder();
        configure(packetBuilder);

        if (builder.Properties.TryGetValue(nameof(PacketBuilder), out var existing) && existing is IPacketProcessorBuilder existingBuilder)
        {
            configure(existingBuilder);
            return builder;
        }

        builder.Properties.Add(nameof(PacketBuilder), packetBuilder);

        return builder.Use((ctx, next) =>
        {
            ctx.Features.Set<IPacketCollectionFeature>(packetBuilder);
            return next(ctx);
        });
    }

    private class PacketBuilder : IPacketProcessorBuilder, IPacketCollectionFeature
    {
        private readonly Dictionary<TdsType, ITdsPacket> _lookup = new();

        public void AddPacket(TdsType type, Action<IPacketBuilder> builder)
        {
            var options = new OptionsBuilder(type);

            builder(options);

            _lookup.Add(type, options);
        }

        public ITdsPacket? Get(TdsType type)
            => _lookup.TryGetValue(type, out var packet) ? packet : null;
    }

    public static IPacketProcessorBuilder AddPreLogin(this IPacketProcessorBuilder builder)
    {
        const byte VERSION = 0;
        const byte ENCRYPT = 1;
        const byte INSTANCE = 2;
        const byte THREADID = 3;
        const byte MARS = 4;
        const byte TRACEID = 5;
        const byte FEDAUTHREQUIRED = 6;

        var version = typeof(IPacketProcessorBuilder).Assembly.GetName().Version ?? Version.Parse("0.0.0");
        var End = new byte[] { 0xFF };
        var DefaultServer = new byte[] { 0x0 };

        builder.AddPacket(TdsType.PreLogin, options =>
        {
            options.Add(VERSION, (_, writer) => writer.Write(version));
            options.Add(ENCRYPT, (_, writer) => writer.Write((byte)0));
            options.Add(INSTANCE, (_, writer) => writer.WriteNullTerminated(string.Empty));
            options.Add(THREADID, (_, writer) => writer.Write((Int32)Environment.CurrentManagedThreadId));
            options.Add(MARS, (_, writer) => writer.Write(true));
            options.Add(TRACEID, (ctx, writer) =>
            {
                if (ctx.TraceId is { } traceId && ctx.Features.Get<ICorrelationFeature>() is { } feature)
                {
                    writer.Write(traceId);
                    writer.Write(feature.ActivityId);
                    writer.Write(feature.SequenceId);
                }
            });
            options.Add(FEDAUTHREQUIRED, (ctx, writer) => writer.Write(true));
            //{ NONCEOPT, (ctx, writer) => writer.Write(ctx.GetNonce()) },
        });

        return builder;
    }
}
