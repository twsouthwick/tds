using Microsoft.Extensions.ObjectPool;
using Microsoft.Protocols.Tds;
using System.Buffers;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds.Packets;

internal class TdsPacketBuilder(TdsType type, ObjectPool<ArrayBufferWriter<byte>> pool, ITdsConnectionBuilder builder) : ITdsPacket, IPacketBuilder
{
    private readonly List<IPacketOption> _items = [];
    private TdsConnectionDelegate? _next;

    public TdsType Type => type;

    IPacketBuilder IPacketBuilder.AddOption(IPacketOption option)
    {
        _items.Add(option);
        return this;
    }

    void ITdsPacket.Write(TdsConnectionContext context, IBufferWriter<byte> writer)
    {
        var offset = Marshal.SizeOf<TdsOptionItem>() * _items.Count + 1;
        var options = pool.Get();
        var payload = pool.Get();

        try
        {
            byte key = 0;

            foreach (var option in _items)
            {
                var before = payload.WrittenCount;

                option.Write(context, payload);

                var after = payload.WrittenCount;

                var optionItem = new TdsOptionItem
                {
                    Type = key++,
                    Offset = (ushort)(offset + before),
                    Length = (ushort)(after - before),
                };

                options.Write(ref optionItem);
            }

            // Mark end of options
            options.Write((byte)255);

            WriteHeader(writer, (short)(options.WrittenCount + payload.WrittenCount));

            writer.Write(options.WrittenSpan);
            writer.Write(payload.WrittenSpan);
        }
        finally
        {
            pool.Return(options);
            pool.Return(payload);
        }
    }

    void ITdsPacket.Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
    {
        if (data.Length == 0)
        {
            return;
        }

        var reader = new OptionsReader(data);

        var count = 0;
        foreach (var item in reader)
        {
            var current = _items[count++];
            current.Read(context, item);
        }
    }

    ValueTask ITdsPacket.OnReadCompleteAsync(TdsConnectionContext context)
    {
        if (_next is { })
        {
            return _next(context);
        }

#if NET
        return ValueTask.CompletedTask;
#else
        return default;
#endif
    }

    private void WriteHeader(IBufferWriter<byte> writer, short length)
    {
        TdsOptionsHeader header = default;

        const int PacketLength = 8;

        header.Type = (byte)type;
        header.Status = (byte)TdsStatus.EOM;
        header.PacketId = 1;
        header.SetLength((short)(length + PacketLength));

        writer.Write(ref header);
    }

    IPacketBuilder IPacketBuilder.AddHandler(Action<ITdsConnectionBuilder> configure)
    {
        if (_next is not null)
        {
            throw new InvalidOperationException("Only a single handler is supported.");
        }

        configure(builder);
        _next = builder.Build();

        return this;
    }

    string ITdsPacket.ToString(ReadOnlyMemory<byte> data)
    {
        var sw = new StringWriter();
        var writer = new IndentedTextWriter(sw);

        try
        {
            writer.WriteLine("[");
            writer.Indent += 1;

            writer.Write("// Header");
            WriteHex(writer, data.Span.Slice(0, 8));

            writer.WriteLine();

            var current = data.Span.Slice(8);

            var optionCount = 0;
            for (int i = 0; i < current.Length && current[i] != 0xFF; i += 5)
            {
                WriteName(writer, "HEADER", _items[optionCount++]);
                WriteHex(writer, current.Slice(i, 5));
            }

            writer.WriteLine();
            writer.WriteLine("0xFF,");

            var reader = new OptionsReader(new ReadOnlySequence<byte>(data));

            writer.WriteLine();
            optionCount = 0;
            foreach (var contents in reader)
            {
                WriteName(writer, "DATA", _items[optionCount++]);
                WriteHex(writer, contents.ToArray());
            }

            static void WriteHex(TextWriter writer, ReadOnlySpan<byte> span)
            {
                if (span.Length == 0)
                {
                    writer.WriteLine();
                    return;
                }

                var count = 0;
                foreach (var item in span)
                {
                    if (count++ % 8 == 0)
                    {
                        writer.WriteLine();
                    }
                    else
                    {
                        writer.Write(", ");
                    }

                    writer.Write("0x");
                    writer.Write(item.ToString("X2"));
                }
                writer.Write(", ");
                writer.WriteLine();
            }

            writer.Indent--;
            writer.Write("]");
        }
        catch
        {
            writer.WriteLine();
            writer.Write("// INVALID PACKET!");
        }

        return sw.ToString();

        static void WriteName(TextWriter writer, string header, IPacketOption packet)
        {
            writer.Write("// ");
            writer.Write(header);
            writer.Write(' ');

            var name = packet.GetType().Name;

            if (name.EndsWith("Option", StringComparison.OrdinalIgnoreCase))
            {
                writer.Write(name.Substring(0, name.Length - "Option".Length));
            }
            else
            {
                writer.Write(name);
            }
        }
    }
}
