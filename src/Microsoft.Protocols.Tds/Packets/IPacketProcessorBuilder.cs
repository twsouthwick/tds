namespace Microsoft.Protocols.Tds.Packets;

public interface IPacketCollectionBuilder
{
    void AddPacket(TdsType type, Action<IPacketBuilder> configure);
}
