using Microsoft.Protocols.Tds.Features;

namespace Microsoft.Protocols.Tds.Packets;

public static class PacketExtensions
{
    public static ITdsConnectionBuilder UseDefaultPacketProcessor(this ITdsConnectionBuilder builder)
        => builder.UsePacketProcessor(packet =>
        {
            packet.AddPreLogin();
        });

    public static ITdsConnectionBuilder UsePacketProcessor(this ITdsConnectionBuilder builder, Action<IPacketBuilder> configure)
    {
        var packetBuilder = new PacketBuilder();
        configure(packetBuilder);

        return builder.Use((ctx, next) =>
        {
            ctx.Features.Set<IPacketCollectionFeature>(packetBuilder);
            return next(ctx);
        });
    }

    private class PacketBuilder : IPacketBuilder, IPacketCollectionFeature
    {
        private readonly Dictionary<TdsType, ITdsPacket> _lookup = new();

        public void AddPacket(TdsType type, Action<OptionsBuilder> builder)
        {
            var options = new OptionsBuilder(type);

            builder(options);

            _lookup.Add(type, options);
        }

        public ITdsPacket? Get(TdsType type)
            => _lookup.TryGetValue(type, out var packet) ? packet : null;
    }

    public static IPacketBuilder AddPreLogin(this IPacketBuilder builder)
    {
        const byte VERSION = 0;
        const byte ENCRYPT = 1;
        const byte INSTANCE = 2;
        const byte THREADID = 3;
        const byte MARS = 4;
        const byte TRACEID = 5;
        const byte FEDAUTHREQUIRED = 6;

        var version = typeof(IPacketBuilder).Assembly.GetName().Version ?? Version.Parse("0.0.0");
        var End = new byte[] { 0xFF };
        var DefaultServer = new byte[] { 0x0 };

        builder.AddPacket(TdsType.PreLogin, options =>
        {
            options.Add(VERSION, (_, writer) => writer.Write(version));
            options.Add(ENCRYPT, (_, writer) => writer.Write((byte)0));
            //options.Add(INSTANCE, (_, writer) => writer.WriteNullTerminated(string.Empty));
            options.Add(THREADID, (_, writer) => writer.Write((Int32)Environment.CurrentManagedThreadId));
            //options.Add(MARS, (_, writer) => writer.Write(false));
            options.Add(TRACEID, (ctx, writer) => writer.Write(ctx.TraceId));
            //options.Add(FEDAUTHREQUIRED, (ctx, writer) => writer.Write(true));
            //{ NONCEOPT, (ctx, writer) => writer.Write(ctx.GetNonce()) },
        });

        return builder;
    }
}
