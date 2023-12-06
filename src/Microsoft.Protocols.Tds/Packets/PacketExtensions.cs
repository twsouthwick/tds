using Microsoft.Extensions.ObjectPool;
using Microsoft.Protocols.Tds.Features;
using System.Buffers;

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

    private class PacketBuilder : IPacketProcessorBuilder, IPacketCollectionFeature, IPooledObjectPolicy<ArrayBufferWriter<byte>>
    {
        private readonly Dictionary<TdsType, ITdsPacket> _lookup;
        private readonly ObjectPool<ArrayBufferWriter<byte>> _pool;

        public PacketBuilder()
        {
            _lookup = new();
            _pool = new DefaultObjectPool<ArrayBufferWriter<byte>>(this);
        }

        public void AddPacket(TdsType type, Action<IPacketOptionBuilder> builder)
        {
            var options = new TdsPacketBuilder(type, _pool);

            builder(options);

            _lookup.Add(type, options);
        }

        public ITdsPacket? Get(TdsType type)
            => _lookup.TryGetValue(type, out var packet) ? packet : null;

        ArrayBufferWriter<byte> IPooledObjectPolicy<ArrayBufferWriter<byte>>.Create()
            => new ArrayBufferWriter<byte>();

        bool IPooledObjectPolicy<ArrayBufferWriter<byte>>.Return(ArrayBufferWriter<byte> obj)
        {
            obj.Clear();
            return true;
        }
    }
}
