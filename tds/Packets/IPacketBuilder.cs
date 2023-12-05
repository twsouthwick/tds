namespace Microsoft.Protocols.Tds.Packets;

public interface IPacketBuilder
{
    void AddPacket(TdsType type, Action<OptionsBuilder> builder);
}
