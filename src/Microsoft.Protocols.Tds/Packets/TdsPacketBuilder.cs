using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using System.Buffers;

namespace Microsoft.Protocols.Tds.Packets;

internal class TdsPacketBuilder(TdsType type, ITdsConnectionBuilder builder) : IPacketBuilder
{
    private readonly List<Func<WriterDelegate, WriterDelegate>> _middleware = new();

    public IServiceProvider Services => builder.Services;

    public ITdsPacket Build()
    {
        WriterDelegate end = (_, _) => { };

        for (int i = _middleware.Count - 1; i >= 0; i--)
        {
            end = _middleware[i](end);
        }

        return new ConfiguredTdsPacket(type, builder.Services.GetRequiredService<ObjectPool<ArrayBufferWriter<byte>>>(), end);
    }

    public IPacketBuilder Use(Func<WriterDelegate, WriterDelegate> middleware)
    {
        _middleware.Add(middleware);
        return this;
    }
}

public delegate void WriterDelegate(TdsConnectionContext context, IBufferWriter<byte> writer);

internal delegate void ReaderDelegate(TdsConnectionContext context, IBufferWriter<byte> writer);
