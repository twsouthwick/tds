using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Protocols.Tds;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds.Packets;

public static class PacketBuilderExtensions
{
    public static IPacketBuilder UseWrite(this IPacketBuilder builder, Action<TdsConnectionContext, IBufferWriter<byte>, WriterDelegate> middleware)
        => builder.UseWrite(next => (ctx, writer) => middleware(ctx, writer, next));

    public static IPacketBuilder UseRead(this IPacketBuilder builder, Action<TdsConnectionContext, ReadOnlySequence<byte>, ReaderDelegate> middleware)
        => builder.UseRead(next =>
        {
            void Reader(TdsConnectionContext context, in ReadOnlySequence<byte> data)
            {
                middleware(context, data, next);
            }

            return Reader;
        });

    public static IPacketBuilder AddOption(this IPacketBuilder builder, Action<IList<IPacketOption>> configure)
    {
        var options = new List<IPacketOption>();

        configure(options);

        var pool = builder.GetBufferWriterPool();

        builder.UseRead(next =>
        {
            void ReadPacket(TdsConnectionContext context, in ReadOnlySequence<byte> data)
            {
                var count = 0;
                foreach (var item in new OptionsReader(data))
                {
                    options[count++].Read(context, item);
                }
            }

            return ReadPacket;
        });

        builder.UseWrite((context, writer, next) =>
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

            next(context, writer);
        });
        return builder;
    }

    public static IPacketBuilder UseLength(this IPacketBuilder builder)
    {
        var pool = builder.GetBufferWriterPool();

        return builder.UseWrite((ctx, w, next) =>
        {
            var payloadWriter = pool.Get();

            try
            {
                next(ctx, payloadWriter);

                w.WriteLittleEndian(payloadWriter.WrittenCount + 4);
                w.Write(payloadWriter.WrittenSpan);
            }
            finally
            {
                pool.Return(payloadWriter);
            }
        });
    }

    internal static ObjectPool<ArrayBufferWriter<byte>> GetBufferWriterPool(this IPacketBuilder builder)
        => builder.Services.GetRequiredService<ObjectPool<ArrayBufferWriter<byte>>>();
}

