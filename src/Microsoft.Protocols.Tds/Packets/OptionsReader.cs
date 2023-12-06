using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds.Packets;

internal struct OptionsReader
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
