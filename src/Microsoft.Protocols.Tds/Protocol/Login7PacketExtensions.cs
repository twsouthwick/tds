using Microsoft.Protocols.Tds.Packets;

namespace Microsoft.Protocols.Tds.Protocol;
public static class Login7PacketExtensions
{
    /// <summary>
    /// Adds implementations for the <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-tds/773a62b6-ee89-4c02-9e5e-344882630aac">LOGIN7</see> packet
    /// </summary>
    public static void AddLogin7(this IPacketCollectionBuilder builder)
    {
        builder.AddPacket(TdsType.Login7, packet =>
        {
            var pool = packet.GetBufferWriterPool();

            packet.UseLengthPrefix((context, writer) =>
            {
                // TDS Version
                writer.Write(0x00_00_00_80);

                // Packet Size
                writer.Write(0x00_10_00_00);

                // ClientProgVer
                writer.Write(0x00_00_00_07);

                // ClientPID
                writer.Write(0x00_00_00_01);

                // ConnectionID
                writer.Write(0x00_00_00_01);

                // OptionFlag1
                var optionFlag1 = OptionFlag1.None;
                writer.Write((byte)optionFlag1);

                // OptionFlag2
                writer.Write((byte)0);

                // TypeFlag
                writer.Write((byte)0);

                // OptionFlag3
                writer.Write((byte)0);

                // ClientTimeZone
                writer.Write((int)0);

                // ClientLCI
                writer.Write((int)0);
            });
        });
    }

    [Flags]
    private enum OptionFlag1 : Byte
    {
        None = 0,
        fByteOrder = 1 << 0,
        fChar = 1 << 1,
        fFloat1 = 1 << 2,
        fFloat2 = 1 << 3,
        fDumpLoad = 1 << 4,
        fUseDB = 1 << 5,
        fDatabase = 1 << 6,
        fSetLang = 1 << 7,
    }
}
