namespace Microsoft.Protocols.Tds.Packets;

public interface IPacketBuilder
{
    IServiceProvider Services { get; }

    IPacketBuilder UseWrite(Func<WriterDelegate, WriterDelegate> middleware);

    IPacketBuilder UseRead(Func<ReaderDelegate, ReaderDelegate> middleware);

    ITdsPacket Build();
}
