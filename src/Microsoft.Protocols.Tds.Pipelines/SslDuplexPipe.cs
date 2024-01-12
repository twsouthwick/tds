using System.IO.Pipelines;
using System.Net.Security;

namespace Microsoft.Protocols.Tds;

internal sealed class SslDuplexPipe : DuplexPipeStreamAdapter<SslStream>
{
    private readonly Swappable _swappable;

    public SslDuplexPipe(IDuplexPipe duplexPipe)
        : this(new(duplexPipe))
    {
    }

    private SslDuplexPipe(Swappable swappable)
        : base(swappable, s => new SslStream(s))
    {
        _swappable = swappable;
    }

    public IDuplexPipe InnerPipe
    {
        get => _swappable.InnerPipe;
        set => _swappable.InnerPipe = value;
    }

    private sealed class Swappable(IDuplexPipe pipe) : IDuplexPipe
    {
        public IDuplexPipe InnerPipe { get; set; } = pipe;

        public PipeReader Input => InnerPipe.Input;

        public PipeWriter Output => InnerPipe.Output;
    }
}
