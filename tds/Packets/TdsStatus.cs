namespace Microsoft.Protocols.Tds.Packets;

// https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-tds/ce398f9a-7d47-4ede-8f36-9dd6fc21ca43
public enum TdsStatus : byte
{
    Normal = 0,
    EOM = 0x01,
    Ignore = 0x02,
    ResetConnection = 0x08,
    ResetConnectionSkipTransaction = 0x10,
}
