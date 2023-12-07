using Microsoft.Extensions.ObjectPool;
using Microsoft.Protocols.Tds;
using System.Buffers;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds.Packets;

internal class TdsPacketBuilder(TdsType type, ObjectPool<ArrayBufferWriter<byte>> pool, ITdsConnectionBuilder builder) : ITdsPacket, IPacketBuilder
{
    private const string Header = "HEADER";

    private readonly List<(IPacketOption, string)> _items = [];
    private TdsConnectionDelegate? _next;

    public TdsType Type => type;

    IPacketBuilder IPacketBuilder.AddOption(IPacketOption option)
    {
        _items.Add((option, GetName(option)));
        return this;

        static string GetName(IPacketOption packet)
        {
            var name = packet.GetType().Name;

            if (name.EndsWith("Option", StringComparison.OrdinalIgnoreCase))
            {
                var length = name.Length - "Option".Length;
                return name.Substring(0, length);
            }
            else
            {
                return name;
            }
        }
    }

    void ITdsPacket.Write(TdsConnectionContext context, IBufferWriter<byte> writer)
    {
        var offset = Marshal.SizeOf<TdsOptionItem>() * _items.Count + 1;
        var options = pool.Get();
        var payload = pool.Get();

        try
        {
            byte key = 0;

            foreach (var (option, _) in _items)
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
            var (packet, _) = _items[count++];
            packet.Read(context, item);
        }
    }

    ValueTask ITdsPacket.OnReadCompleteAsync(TdsConnectionContext context)
        => _next is { } ? _next(context) : default;

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

    string ITdsPacket.ToString(ReadOnlyMemory<byte> data, TdsPacketFormattingOptions options) => options switch
    {
        TdsPacketFormattingOptions.Code => CodeToString(data),
        _ => DefaultToString(data),
    };

    private string DefaultToString(ReadOnlyMemory<byte> data)
    {
        var sw = new StringWriter();
        var writer = new IndentedTextWriter(sw, " ");
        var length = _items.Max(i => i.Item2.Length) + 1;

        writer.Write(Header);
        Padding(writer, Header, length);
        WriteHex(writer, data.Span.Slice(0, 8), TdsPacketFormattingOptions.Default);
        writer.WriteLine();
        writer.WriteLine("-------------");

        var count = 0;
        foreach (var option in new OptionsReader(new ReadOnlySequence<byte>(data)))
        {
            var (_, name) = _items[count++];

            writer.Write(name);
            var padding = Padding(writer, name, length);
            writer.Indent += padding;
            WriteHex(writer, option.ToArray(), TdsPacketFormattingOptions.Default);
            writer.Indent -= padding;
            writer.WriteLine();
        }

        return sw.ToString();

        static int Padding(TextWriter writer, string name, int length)
        {
            var padding = Math.Min(length - name.Length, length);
            writer.Write(new string(' ', padding));
            return padding;
        }
    }

    private string CodeToString(ReadOnlyMemory<byte> data)
    {
        var sw = new StringWriter();
        var writer = new IndentedTextWriter(sw);

        try
        {
            writer.WriteLine("[");
            writer.Indent++;

            writer.WriteLine($"// {Header}");
            WriteHex(writer, data.Span.Slice(0, 8), TdsPacketFormattingOptions.Code);

            writer.WriteLine();

            var current = data.Span.Slice(8);

            var optionCount = 0;
            for (int i = 0; i < current.Length && current[i] != 0xFF; i += 5)
            {
                WriteName(writer, "OPTION", optionCount++);
                WriteHex(writer, current.Slice(i, 5), TdsPacketFormattingOptions.Code);
            }

            writer.WriteLine();
            writer.WriteLine("0xFF,");

            var reader = new OptionsReader(new ReadOnlySequence<byte>(data));

            writer.WriteLine();
            optionCount = 0;
            foreach (var contents in reader)
            {
                WriteName(writer, "DATA", optionCount++);
                WriteHex(writer, contents.ToArray(), TdsPacketFormattingOptions.Code);
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
    }

    private void WriteName(TextWriter writer, string header, int count)
    {
        writer.Write("// ");
        writer.Write(header);
        writer.Write(' ');
        writer.WriteLine(_items[count].Item2);
    }

    private static void WriteHex(TextWriter writer, ReadOnlySpan<byte> span, TdsPacketFormattingOptions options)
    {
        if (span.Length == 0)
        {
            if (options == TdsPacketFormattingOptions.Code)
            {
                writer.WriteLine();
            }
            return;
        }

        var count = 0;
        foreach (var item in span)
        {
            if (count == 0)
            {

            }
            else if (count % 8 == 0)
            {
                writer.WriteLine();
            }
            else if (options == TdsPacketFormattingOptions.Code)
            {
                writer.Write(", ");
            }
            else
            {
                writer.Write(" ");
            }

            count++;

            if (options == TdsPacketFormattingOptions.Code)
            {
                writer.Write("0x");
            }

            writer.Write(item.ToString("X2"));
        }

        if (options == TdsPacketFormattingOptions.Code)
        {
            writer.WriteLine(", ");
        }
    }
}
