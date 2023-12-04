using System.IO.Pipelines;

namespace Microsoft.Protocols.Tds.Features;

public interface ITdsConnectionFeature
{
    IDuplexPipe Pipe { get; }
}
