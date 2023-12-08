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
            packet.AddLogin7();
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

    private class PacketCollectionBuilder : IPacketCollectionBuilder, IPacketCollectionFeature, IPooledObjectPolicy<ArrayBufferWriter<byte>>
    {
        private readonly Dictionary<TdsType, ITdsPacket> _lookup;
        private readonly ObjectPool<ArrayBufferWriter<byte>> _pool;
        private readonly ITdsConnectionBuilder _builder;

        public PacketCollectionBuilder(ITdsConnectionBuilder builder)
        {
            _lookup = new();
            _pool = new DefaultObjectPool<ArrayBufferWriter<byte>>(this);
            _builder = builder;
        }

        public ITdsPacket? Get(TdsType type)
            => _lookup.TryGetValue(type, out var packet) ? packet : null;

        void IPacketCollectionBuilder.AddPacket(TdsType type, Action<IPacketBuilder> configure)
        {
            var options = new TdsPacketBuilder(type, _pool, _builder.New());

            configure(options);

            _lookup.Add(type, options.Build());
        }

        ArrayBufferWriter<byte> IPooledObjectPolicy<ArrayBufferWriter<byte>>.Create()
            => new ArrayBufferWriter<byte>();

        bool IPooledObjectPolicy<ArrayBufferWriter<byte>>.Return(ArrayBufferWriter<byte> obj)
        {
            obj.ResetWrittenCount();
            return true;
        }
    }
}
