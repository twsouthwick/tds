using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds.Packets;

public class ParsingContext
{
    private readonly ReadOnlySequence<byte> _original;

    private FeatureReference<IPacketParserFeature> _parser = FeatureReference<IPacketParserFeature>.Default;

    public ParsingContext(TdsConnectionContext context, in ReadOnlySequence<byte> input, SequencePosition consumed, SequencePosition examined)
    {
        _original = input;

        Context = context;
        Input = input;
        Consumed = consumed;
        Examined = examined;
    }

    public TdsConnectionContext Context { get; }

    public ReadOnlySequence<byte> Input { get; private set; }

    public SequencePosition Consumed { get; private set; }

    public SequencePosition Examined { get; private set; }

    public void Advance(int count)
    {
        Input = Input.Slice(count);
        Consumed = Input.Start;
        Examined = Consumed;
    }

    private IPacketParserFeature Parser => _parser.Fetch(Context.Features) ?? throw new InvalidOperationException();

    public TdsResponsePacket? Parse(TdsType type) => Parser.Parse(type, this);

    public TdsResponsePacket? Parse() => Parser.Parse(this);

    public T Read<T>()
        where T : struct
    {
        var buffer = Input.Slice(0, Marshal.SizeOf<T>());

        if (!buffer.IsSingleSegment)
        {
            throw new InvalidOperationException("Only supports single segments right now");
        }

        var result = MemoryMarshal.Read<T>(buffer.First.Span);

        Advance(Marshal.SizeOf<T>());

        return result;
    }
}

