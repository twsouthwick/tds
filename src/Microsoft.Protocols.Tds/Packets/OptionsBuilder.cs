using Microsoft.Extensions.ObjectPool;
using Microsoft.Protocols.Tds;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds.Packets;

internal class OptionsBuilder(TdsType type, ObjectPool<ArrayBufferWriter<byte>> pool) : ITdsPacket, IPacketOptionBuilder
{
    private readonly List<IPacketOption> _items = new();

    public TdsType Type => type;

    public void Add(IPacketOption option)
        => _items.Add(option);

    public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
    {
        var offset = 5 * _items.Count + 1;
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

    public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct TdsOptionItem
    {
        public byte Type { get; set; }

        public ushort offset_value { get; set; }

        public ushort length_value { get; set; }

        public ushort Offset
        {
            get => BinaryPrimitives.ReverseEndianness(offset_value);
            set => offset_value = BinaryPrimitives.ReverseEndianness(value);
        }

        public ushort Length
        {
            get => BinaryPrimitives.ReverseEndianness(length_value);
            set => length_value = BinaryPrimitives.ReverseEndianness(value);
        }
    }

    private struct OptionsReader
    {
        private readonly TdsOptionsHeader _header;
        private readonly ReadOnlySequence<byte> _data;

        public OptionsReader(in ReadOnlySequence<byte> data)
        {
            var reader = new SequenceReader<byte>(data);
            _header = reader.Read<TdsOptionsHeader>();
            _data = data.Slice(Marshal.SizeOf<TdsOptionsHeader>());
        }

        public Enumerator GetEnumerator() => new Enumerator(_data);

        public struct Enumerator(ReadOnlySequence<byte> data)
        {
            private int _count = -1;

            public ReadOnlySequence<byte> Current
            {
                get
                {
                    var slice = data.Slice(_count * Marshal.SizeOf<TdsOptionItem>(), Marshal.SizeOf<TdsOptionItem>());
                    var reader = new SequenceReader<byte>(slice);
                    var item = reader.Read<TdsOptionItem>();

                    return data.Slice(item.Offset, item.Length);
                }
            }

            private bool HasEnded
            {
                get
                {
                    if (_count == -1)
                    {
                        return false;
                    }

                    var section = data.Slice(_count * Marshal.SizeOf<TdsOptionItem>(), Marshal.SizeOf<TdsOptionItem>());

                    return section.Length == 0 || section.First.Span[0] == 0xFF;
                }
            }

            public bool MoveNext()
            {
                Debug.Assert(!HasEnded);

                ++_count;

                return !HasEnded;
            }
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
