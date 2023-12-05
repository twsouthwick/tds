namespace Microsoft.Protocols.Tds.Packets;

public class TableTdsResponsePacket : TdsResponsePacket
{
    public TableTdsResponsePacket(List<TdsResponsePacket> list) : base(TdsType.Table)
    {
        Rows = list;
    }

    public IReadOnlyList<TdsResponsePacket> Rows { get; }

    public static new TableTdsResponsePacket Parse(ParsingContext context)
    {
        var list = new List<TdsResponsePacket>();

        while (true)
        {
            if (context.ParseNext() is { } parsed)
            {
                list.Add(parsed);
            }
            else
            {
                break;
            }
        }

        return new(list);
    }
}

