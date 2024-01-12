using System.Buffers;

namespace Microsoft.Protocols.Tds.Packets;

internal class TdsPacketBuilder(TdsType type, ITdsConnectionBuilder builder) : IPacketBuilder
{
    private readonly List<Func<WriterDelegate, WriterDelegate>> _writerMiddleware = [];
    private readonly List<Func<ReaderDelegate, ReaderDelegate>> _readerMiddleware = [];

    private static void ReaderEnd(TdsConnectionContext context, in ReadOnlySequence<byte> input)
    {
    }

    public IServiceProvider Services => builder.Services;

    public ITdsPacket Build()
    {
        WriterDelegate writer = (_, _) => { };

        for (int i = _writerMiddleware.Count - 1; i >= 0; i--)
        {
            writer = _writerMiddleware[i](writer);
        }

        ReaderDelegate reader = ReaderEnd;

        for (int i = _readerMiddleware.Count - 1; i >= 0; i--)
        {
            reader = _readerMiddleware[i](reader);
        }

        return new ConfiguredTdsPacket(type, writer, reader);
    }

    public IPacketBuilder UseRead(Func<ReaderDelegate, ReaderDelegate> middleware)
    {
        _readerMiddleware.Add(middleware);
        return this;
    }

    public IPacketBuilder UseWrite(Func<WriterDelegate, WriterDelegate> middleware)
    {
        _writerMiddleware.Add(middleware);
        return this;
    }
}

public delegate void WriterDelegate(TdsConnectionContext context, IBufferWriter<byte> writer);

public delegate void ReaderDelegate(TdsConnectionContext context, in ReadOnlySequence<byte> input);
