using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;
using System.Buffers;

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

            packet.UseLength();
            packet.Use((context, writer, next) =>
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

                var sqlUser = context.Features.Get<ISqlUserAuthenticationFeature>();
                var env = context.Features.GetRequiredFeature<IEnvironmentFeature>();
                var conn = context.Features.GetRequiredFeature<IConnectionStringFeature>();

                var payload = pool.Get();
                var offset = OffsetWriter.Create(
                    13, // Items added to payload needs to be known up front 
                    writer,
                    payload,
                    additionalCount: 6 + 4); // Additional items are the ClientId and the reserved for chSSPI element

                offset.WritePayload(sqlUser?.HostName ?? string.Empty); // HostName
                offset.WritePayload(sqlUser?.UserName ?? string.Empty); // UserName
                offset.WritePayload(sqlUser?.Password ?? string.Empty); // Password
                offset.WritePayload(env.AppName); // AppName
                offset.WritePayload(env.ServerName); // ServerName
                offset.WritePayload(); // Unused
                offset.WritePayload(); // Extension
                offset.WritePayload(); // CltIntName
                offset.WritePayload(); // Language
                offset.WritePayload(conn.Database); // Database

                if (env.ClientId is not { Length: 6 })
                {
                    throw new InvalidOperationException("ClientID must be 6 bytes");
                }

                offset.WriteOffset(env.ClientId); // ClientId - NIC?

                if (context.Features.Get<ISspiAuthenticationFeature>() is { } sspi)
                {
                    offset.WritePayload(sspi);
                }
                else
                {
                    offset.WritePayload(); // SSPI
                }

                offset.WritePayload(); // AtchDBFile
                offset.WritePayload(); // ChangePassword

                offset.WriteOffset(new byte[] { 0, 0, 0, }); // reserved for chSSPI

                offset.Complete();

                next(context, writer);
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
