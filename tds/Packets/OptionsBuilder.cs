﻿using Microsoft.Extensions.ObjectPool;
using Microsoft.Protocols.Tds;
using System.Buffers;

namespace Microsoft.Protocols.Tds.Packets;

internal class OptionsBuilder(TdsType type, ObjectPool<ArrayBufferWriter<byte>> pool) : ITdsPacket, IPacketBuilder
{
    private readonly List<(byte Key, Action<TdsConnectionContext, IBufferWriter<byte>> Writer)> _items = new();

    public TdsType Type => type;

    public void Add(byte optionKey, Action<TdsConnectionContext, IBufferWriter<byte>> func)
        => _items.Add((optionKey, func));

    public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
    {
        var offset = 5 * _items.Count + 1;
        var options = pool.Get();
        var payload = pool.Get();

        try
        {
            foreach (var (key, func) in _items)
            {
                var before = payload.WrittenCount;

                func(context, payload);

                var after = payload.WrittenCount;

                options.Write((byte)key);
                options.Write((short)(offset + before));
                options.Write((short)(after - before));
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

    private void WriteHeader(IBufferWriter<byte> writer, short length)
    {
        const int PacketLength = 8;
        var header = writer.GetSpan(PacketLength);

        header[0] = (byte)type;
        header[1] = (byte)TdsStatus.EOM;

        var totalLength = (short)(length + PacketLength);
        header.Slice(2, 2).Write(totalLength);

        header[6] = 1; // packet id

        writer.Advance(PacketLength);
    }
}
