namespace Microsoft.Protocols.Tds.Packets;

public enum TdsType : byte
{
    Unknown = 0,
    Table = 0x04,
    PreLogin = 0x12,
    Done = 0xFD,
}
