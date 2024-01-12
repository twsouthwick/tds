using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using System.Buffers;

namespace Microsoft.Protocols.Tds.Packets;

internal class TdsPacketBuilder(TdsType type, ITdsConnectionBuilder builder) : IPacketBuilder
{
    private ITdsConnectionBuilder? _send;
    private readonly List<Func<WriterDelegate, WriterDelegate>> _writerMiddleware = [];
    private readonly List<Func<ReaderDelegate, ReaderDelegate>> _readerMiddleware = [];

    private static void ReaderEnd(TdsConnectionContext context, in ReadOnlySequence<byte> input)
    {
    }

    public IServiceProvider Services => builder.Services;

    public ITdsConnectionBuilder Send => _send ??= builder.New();

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

        var send = _send?.Build() ?? DefaultSend;

        return new ConfiguredTdsPacket(type, writer, reader, send);
    }

    public IPacketBuilder UseRead(Func<ReaderDelegate, ReaderDelegate> middleware)
    {
        _readerMiddleware.Add(middleware);
        return this;
    }

    private async ValueTask DefaultSend(TdsConnectionContext context)
    {
        var feature = context.Features.GetRequiredFeature<ITdsConnectionFeature>();

        var packet = context.GetPacket(type);

        await feature.WritePacket(packet);
        await feature.ReadPacketAsync(packet);
    }

    public IPacketBuilder UseWrite(Func<WriterDelegate, WriterDelegate> middleware)
    {
        _writerMiddleware.Add(middleware);
        return this;
    }
}

public delegate void WriterDelegate(TdsConnectionContext context, IBufferWriter<byte> writer);

public delegate void ReaderDelegate(TdsConnectionContext context, in ReadOnlySequence<byte> input);
