namespace Microsoft.Protocols.Tds.Packets;

public interface IPacketBuilder
{
    IServiceProvider Services { get; }

    IPacketBuilder Use(Func<WriterDelegate, WriterDelegate> middleware);

    IPacketBuilder AddHandler(Action<ITdsConnectionBuilder> builder);

    ITdsPacket Build();
}
