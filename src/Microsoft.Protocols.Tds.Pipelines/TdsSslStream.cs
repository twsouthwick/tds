using System.IO.Pipelines;
using System.Net.Security;

namespace Microsoft.Protocols.Tds;

internal sealed class SslDuplexAdapter(IDuplexPipe duplexPipe) : DuplexPipeStreamAdapter<SslStream>(duplexPipe, s => new SslStream(s))
{
}
