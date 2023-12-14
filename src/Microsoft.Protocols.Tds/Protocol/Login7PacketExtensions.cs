using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;
using System.Buffers;
using System.Globalization;

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
            var chSSPI = new byte[] { 0, 0, 0, 0 };
            var pool = packet.GetBufferWriterPool();

            packet.UseLength();
            packet.UseWrite((context, writer, next) =>
            {
                var env = context.Features.GetRequiredFeature<IEnvironmentFeature>();
                var conn = context.Features.GetRequiredFeature<IConnectionStringFeature>();

                // TDS Version
                writer.WriteLittleEndian((0x08 << 24));

                // Packet Size
                writer.WriteLittleEndian(0x00_10_00_00);

                // ClientProgVer
                writer.WriteLittleEndian(0x00_00_00_07);

                // ClientPID
                writer.WriteLittleEndian(env.ProcessId);

                // ConnectionID
                writer.WriteLittleEndian(0x00_00_00_00);

                // OptionFlag1
                var optionFlag1 = (OptionFlag1)0xE0;
                writer.Write((byte)optionFlag1);

                // OptionFlag2
                writer.Write((byte)0x03);

                // TypeFlag
                writer.Write((byte)0);

                // OptionFlag3
                writer.Write((byte)0);

                // ClientTimeZone -- NOT USED
                writer.WriteLittleEndian((int)0);

                // ClientLCID -- NOT USED
                writer.WriteLittleEndian(0);

                // .UseLength() will set a new ArrayBufferWriter<byte> as the writer
                var initialOffset = ((ArrayBufferWriter<byte>)writer).WrittenCount + 4;
                var payload = pool.Get();
                var offset = OffsetWriter.Create(
                    12, // Items added to payload needs to be known up front 
                    initialOffset,
                    writer,
                    payload,
                    additionalCount: 6 + 4); // Additional items are the ClientId and the reserved for chSSPI element

                if (context.Features.Get<ISqlUserAuthenticationFeature>() is { } sqlUser)
                {
                    offset.WritePayload(sqlUser.HostName);
                    offset.WritePayload(sqlUser.UserName);
                    offset.WritePayload(sqlUser.Password, encrypt: true);
                }
                else
                {
                    offset.WritePayload(); // HostName
                    offset.WritePayload(); // UserName
                    offset.WritePayload(); // Password
                }

                offset.WritePayload(env.AppName); // AppName
                offset.WritePayload(env.ServerName); // ServerName
                offset.WritePayload(); // Unused/Extension
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

                offset.WriteOffset(chSSPI); // reserved for chSSPI

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
