namespace Microsoft.Protocols.Tds.Packets;

public interface IPacketBuilder
{
    IServiceProvider Services { get; }

    IPacketBuilder Use(Func<WriterDelegate, WriterDelegate> middleware);

    ITdsPacket Build();
}
