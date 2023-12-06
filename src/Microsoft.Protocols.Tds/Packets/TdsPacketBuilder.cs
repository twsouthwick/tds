using Microsoft.Extensions.ObjectPool;
using Microsoft.Protocols.Tds;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds.Packets;

internal class TdsPacketBuilder(TdsType type, ObjectPool<ArrayBufferWriter<byte>> pool) : ITdsPacket, IPacketOptionBuilder
{
    private readonly List<IPacketOption> _items = new();

    public TdsType Type => type;

    void IPacketOptionBuilder.Add(IPacketOption option)
        => _items.Add(option);

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
}
