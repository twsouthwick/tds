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

                var payload = pool.Get();
                var items1 = OffsetWriter.Create(10, writer, payload);

                var sqlUser = context.Features.Get<ISqlUserAuthenticationFeature>();
                items1.Add(sqlUser?.HostName ?? string.Empty); // HostName
                items1.Add(sqlUser?.UserName ?? string.Empty); // UserName
                items1.Add(sqlUser?.Password ?? string.Empty); // Password

                var env = context.Features.GetRequiredFeature<IEnvironmentFeature>();
                var conn = context.Features.GetRequiredFeature<IConnectionStringFeature>();

                items1.Add(env.AppName); // AppName
                items1.Add(env.ServerName); // ServerName
                items1.Add(string.Empty); // Unused
                items1.Add(string.Empty); // Extension
                items1.Add(string.Empty); // CltIntName
                items1.Add(string.Empty); // Language
                items1.Add(conn.Database); // Database

                if (env.ClientId is not { Length: 6 })
                {
                    throw new InvalidOperationException("ClientID must be 6 bytes");
                }

                payload.Write(env.ClientId); // ClientId - NIC?

                var items2 = OffsetWriter.Create(3, writer, payload);

                if (context.Features.Get<ISspiAuthenticationFeature>() is { } sspi)
                {
                    var before = payload.WrittenCount;
                    sspi.WriteBlock(Array.Empty<byte>(), payload);
                }
                else
                {
                    items2.Add(string.Empty); // SSPI
                }

                items2.Add(string.Empty); // AtchDBFile
                items2.Add(string.Empty); // ChangePassword

                payload.Write((int)0); // reserved for chSSPI

                writer.Write(payload.WrittenSpan);
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
