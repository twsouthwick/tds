using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.Tds.Packets;

public class ParsingContext
{
    public ParsingContext(TdsConnectionContext context, in ReadOnlySequence<byte> input, SequencePosition consumed, SequencePosition examined)
    {
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

    public TdsResponsePacket? ParseNext() => Context.Features.GetRequiredFeature<IPacketParserFeature>().Parse(this);

    public T Read<T>()
        where T : struct
    {
        var buffer = Input.Slice(0, Marshal.SizeOf<T>());

        if (!buffer.IsSingleSegment)
        {
            throw new InvalidOperationException("Only supports single segments right now");
        }

        var result = MemoryMarshal.Read<T>(buffer.First.Span);


        Consumed = buffer.End;
        Examined = Consumed;

        return result;
    }
}

