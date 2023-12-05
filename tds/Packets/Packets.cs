using System.Buffers;
using System.Security.Cryptography;

namespace Microsoft.Protocols.Tds.Packets;

public static class Packets
{
    public static class PreLogin
    {
        private static Version _version = typeof(Packets).Assembly.GetName().Version ?? Version.Parse("0.0.0");
        private static byte[] End = [0xFF];
        private static byte[] DefaultServer = [0x0];

        public static ITdsPacket Packet { get; } = new OptionsBuilder(TdsType.PreLogin)
        {
            { VERSION, (_,writer) => writer.Write(_version) },
            { ENCRYPT, (_, writer) => writer.Write((byte)0) },
            { INSTANCE, (_, writer) => writer.WriteNullTerminated(string.Empty) },
            { THREADID, (_, writer) => writer.Write((Int32)Environment.CurrentManagedThreadId) },
            { MARS, (_, writer) => writer.Write(false) },
            { TRACEID, (ctx, writer) => writer.Write(ctx.TraceId) },
            { FEDAUTHREQUIRED, (ctx, writer) => writer.Write(true) },
            //{ NONCEOPT, (ctx, writer) => writer.Write(ctx.GetNonce()) },
        };

        private const byte VERSION = 0;
        private const byte ENCRYPT = 1;
        private const byte INSTANCE = 2;
        private const byte THREADID = 3;
        private const byte MARS = 4;
        private const byte TRACEID = 5;
        private const byte FEDAUTHREQUIRED = 6;
        private const byte NONCEOPT = 7;
        private const byte LASTOPT = 255;
    }
}
