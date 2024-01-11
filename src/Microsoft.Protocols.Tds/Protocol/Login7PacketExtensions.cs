using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;
using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;

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
                var useFeatureExtension = false;

                // TDS Version
                var version = 1946157060; // hardcoded from sqlclient
                writer.WriteLittleEndian(version);

                // Packet Size
                writer.WriteLittleEndian(8_000);

                // ClientProgVer
                writer.WriteLittleEndian(100663296);

                // ClientPID
                writer.WriteLittleEndian(env.ProcessId);

                // ConnectionID
                writer.WriteLittleEndian(0);

                // Flags
                var optionFlag1 = useFeatureExtension ? 268436448 : 992;
                writer.WriteLittleEndian(optionFlag1);

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

                if (useFeatureExtension)
                {
                    throw new NotImplementedException();
                    //offset.WritePayload(new[]{4);
                }
                else
                {
                    offset.WritePayload();
                }

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
            })
            .WriteFeatures();
        });
    }

    public static IPacketBuilder WriteFeatures(this IPacketBuilder builder)
    {
        var features = new[] { Recovery, Tce, GlobalTransactions, Utf8 };

        return builder.UseWrite((ctx, writer, next) =>
        {
            foreach (var f in features)
            {
                f(ctx, writer);
            }

            writer.Write((byte)0xFF);

            next(ctx, writer);
        });
    }

    public static void Recovery(TdsConnectionContext context, IBufferWriter<byte> writer)
    {
        writer.Write((byte)0x01);

        // Reconnect data
        writer.GetSpan(4).Clear();
        writer.Advance(4);
    }

    public static void Tce(TdsConnectionContext context, IBufferWriter<byte> writer)
    {
        writer.Write((byte)0x04);

        writer.WriteLittleEndian(1);

        writer.Write((byte)0x03);
    }

    public static void GlobalTransactions(TdsConnectionContext context, IBufferWriter<byte> writer)
    {
        writer.Write((byte)0x05);
        writer.WriteLittleEndian(0);
    }

    public static void Utf8(TdsConnectionContext context, IBufferWriter<byte> writer)
    {
        writer.Write((byte)0x0A);
        writer.WriteLittleEndian(0);
    }
}
