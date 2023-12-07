using Microsoft.Extensions.ObjectPool;
using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Protocol;
using System.Buffers;

namespace Microsoft.Protocols.Tds.Packets;

public static class PacketExtensions
{
    public static ITdsConnectionBuilder UseDefaultPacketProcessor(this ITdsConnectionBuilder builder)
        => builder.UsePacketProcessor(packet =>
        {
            packet.AddPreLogin();
        });

    public static ITdsConnectionBuilder UsePacketProcessor(this ITdsConnectionBuilder builder, Action<IPacketCollectionBuilder> configure)
    {
        var packetBuilder = new PacketBuilder(builder.New());
        configure(packetBuilder);

        if (builder.Properties.TryGetValue(nameof(PacketBuilder), out var existing) && existing is IPacketCollectionBuilder existingBuilder)
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

    private class PacketBuilder : IPacketCollectionBuilder, IPacketCollectionFeature, IPooledObjectPolicy<ArrayBufferWriter<byte>>
    {
        private readonly Dictionary<TdsType, ITdsPacket> _lookup;
        private readonly ObjectPool<ArrayBufferWriter<byte>> _pool;
        private readonly ITdsConnectionBuilder _builder;

        public PacketBuilder(ITdsConnectionBuilder builder)
        {
            _lookup = new();
            _pool = new DefaultObjectPool<ArrayBufferWriter<byte>>(this);
            _builder = builder;
        }

        public ITdsPacket? Get(TdsType type)
            => _lookup.TryGetValue(type, out var packet) ? packet : null;

        IPacketBuilder IPacketCollectionBuilder.AddPacket(TdsType type)
        {
            var options = new TdsPacketBuilder(type, _pool, _builder.New());

            _lookup.Add(type, options);

            return options;
        }

        ArrayBufferWriter<byte> IPooledObjectPolicy<ArrayBufferWriter<byte>>.Create()
            => new ArrayBufferWriter<byte>();

        bool IPooledObjectPolicy<ArrayBufferWriter<byte>>.Return(ArrayBufferWriter<byte> obj)
        {
            obj.Clear();
            return true;
        }
    }
}
