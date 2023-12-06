using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds.Packets;

[StructLayout(LayoutKind.Sequential)]
internal struct TdsOptionsHeader
{
    public byte Type { get; set; }

    public byte Status { get; set; }

    public byte Length1 { get; set; }

    public byte Length2 { get; set; }

    public ushort SPID { get; set; }

    public byte PacketId { get; set; }

    public byte Window { get; set; }

    public int GetLength() => (Length1 << 8) | Length2;

    public void SetLength(int length)
    {
        Length2 = (byte)length;
        Length1 = (byte)(length >> 8);
    }
}

