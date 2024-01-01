using System.IO.Pipelines;

namespace Microsoft.Protocols.Tds;


internal sealed class PacketWrappingDuplexStream(IDuplexPipe other) : IDuplexPipe
{
    public PipeReader Input => other.Input;

    public PipeWriter Output => other.Output;
}
