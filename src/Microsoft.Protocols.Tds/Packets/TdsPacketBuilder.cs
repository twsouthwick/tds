using Microsoft.Extensions.ObjectPool;
using Microsoft.Protocols.Tds;
using System.Buffers;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds.Packets;

internal class TdsPacketBuilder(TdsType type, ObjectPool<ArrayBufferWriter<byte>> pool, ITdsConnectionBuilder builder) : IPacketBuilder
{
    private WriterDelegate? _writer;

    public ObjectPool<ArrayBufferWriter<byte>> Pool => pool;

    public IServiceProvider Services => builder.Services;

    IPacketBuilder IPacketBuilder.AddHandler(Action<ITdsConnectionBuilder> configure)
    {
        configure(builder);
        return this;
    }

    public ITdsPacket Build()
        => new ConfiguredTdsPacket(type, pool, _writer, builder.Build());

    public IPacketBuilder AddWriter(WriterDelegate writer)
    {
        _writer += writer;
        return this;
    }
}

public delegate void WriterDelegate(TdsConnectionContext context, IBufferWriter<byte> writer);

internal delegate void ReaderDelegate(TdsConnectionContext context, IBufferWriter<byte> writer);
