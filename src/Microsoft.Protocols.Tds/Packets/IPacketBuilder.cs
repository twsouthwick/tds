namespace Microsoft.Protocols.Tds.Packets;

public interface IPacketBuilder
{
    IPacketBuilder AddLength();

    IPacketBuilder AddOption(IPacketOption option);

    IPacketBuilder AddHandler(Action<ITdsConnectionBuilder> builder);
}
