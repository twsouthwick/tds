using System.Buffers;

namespace Microsoft.Protocols.Tds.Packets;

public static class BufferHeaderExtensions
{
    public static void WriteHeader(this IBufferWriter<byte> writer, TdsType type, short length)
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
