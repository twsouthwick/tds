namespace Microsoft.Protocols.Tds.Packets;

public class TdsResponsePacket(TdsType type)
{
    public TdsType Type => type;

    public static TdsResponsePacket? Parse(ParsingContext context)
    {
        var header = context.Read<TdsOptionsHeader>();
        var length = header.GetLength();

        if (length == 0)
        {
            return null;
        }

        return context.Parse((TdsType)header.Type);
    }
}
