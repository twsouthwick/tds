using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds.Packets;

public class TdsDonePacket : TdsResponsePacket
{
    private TdsDonePacket(Done done) : base(TdsType.Done)
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Done
    {
        public byte TokenType { get; set; }

        public ushort Status { get; set; }

        public ushort CurCmd { get; set; }

        long RowCount { get; set; }
    }

    public static new TdsDonePacket Parse(ParsingContext context)
       => new(context.Read<Done>());
}

