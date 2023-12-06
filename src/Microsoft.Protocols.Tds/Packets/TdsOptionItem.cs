using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds.Packets;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TdsOptionItem
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
