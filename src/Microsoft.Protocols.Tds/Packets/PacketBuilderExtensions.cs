using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Protocols.Tds;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds.Packets;

public static class PacketBuilderExtensions
{
    public static IPacketBuilder AddWriter(this IPacketBuilder builder, WriterDelegate action)
        => builder.Use((ctx, writer, next) =>
        {
            action(ctx, writer);
            next(ctx, writer);
        });

    public static IPacketBuilder Use(this IPacketBuilder builder, Action<TdsConnectionContext, IBufferWriter<byte>, WriterDelegate> middleware)
        => builder.Use(next => (ctx, writer) => middleware(ctx, writer, next));

    public static IPacketBuilder AddOption(this IPacketBuilder builder, Action<IList<IPacketOption>> configure)
    {
        var options = new List<IPacketOption>();

        configure(options);

        var pool = builder.Services.GetRequiredService<ObjectPool<ArrayBufferWriter<byte>>>();

        builder.AddWriter((context, writer) =>
        {
            var offset = Marshal.SizeOf<TdsOptionItem>() * options.Count + 1;
            var optionsWriter = pool.Get();
            var payloadWriter = pool.Get();

            try
            {
                byte key = 0;

                foreach (var option in options)
                {
                    var before = payloadWriter.WrittenCount;

                    option.Write(context, payloadWriter);

                    var after = payloadWriter.WrittenCount;

                    var optionItem = new TdsOptionItem
                    {
                        Type = key++,
                        Offset = (ushort)(offset + before),
                        Length = (ushort)(after - before),
                    };

                    optionsWriter.Write(ref optionItem);
                }

                // Mark end of options
                optionsWriter.Write((byte)255);

                writer.Write(optionsWriter.WrittenSpan);
                writer.Write(payloadWriter.WrittenSpan);
            }
            finally
            {
                pool.Return(optionsWriter);
                pool.Return(payloadWriter);
            }
        });
        return builder;
    }

    public static IPacketBuilder AddWriter(this IPacketBuilder builder, WriterDelegate writer, bool addLength)
    {
        if (!addLength)
        {
            return builder.AddWriter(writer);
        }
        else
        {
            var pool = builder.Services.GetRequiredService<ObjectPool<ArrayBufferWriter<byte>>>();

            return builder.AddWriter((ctx, w) =>
            {
                var payloadWriter = pool.Get();

                try
                {
                    writer(ctx, payloadWriter);

                    w.Write(payloadWriter.WrittenCount);
                    w.Write(payloadWriter.WrittenSpan);
                }
                finally
                {
                    pool.Return(payloadWriter);
                }
            });
        }

    }
}
