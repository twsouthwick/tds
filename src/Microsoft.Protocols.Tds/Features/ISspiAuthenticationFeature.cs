using System.Buffers;

namespace Microsoft.Protocols.Tds.Features;

public interface ISspiAuthenticationFeature
{
    void WriteBlock(ReadOnlySpan<byte> block, IBufferWriter<byte> writer);
}

