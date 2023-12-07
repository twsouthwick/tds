namespace Microsoft.Protocols.Tds.Packets;

public enum TdsType : byte
{
    Unknown = 0,
    Table = 0x04,
    Login7 = 0x10,
    PreLogin = 0x12,
    Done = 0xFD,
}
