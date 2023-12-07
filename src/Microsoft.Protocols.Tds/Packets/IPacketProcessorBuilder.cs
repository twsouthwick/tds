namespace Microsoft.Protocols.Tds.Packets;

public interface IPacketCollectionBuilder
{
    IPacketBuilder AddPacket(TdsType type);
}
