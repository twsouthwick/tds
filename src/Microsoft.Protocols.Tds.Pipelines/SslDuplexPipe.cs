using System.IO.Pipelines;
using System.Net.Security;

namespace Microsoft.Protocols.Tds;

internal sealed class SslDuplexPipe : DuplexPipeStreamAdapter<SslStream>
{
    private readonly Swappable _swappable;

    public SslDuplexPipe(IDuplexPipe duplexPipe, Func<bool> trust)
        : this(new(duplexPipe), trust)
    {
    }

    private SslDuplexPipe(Swappable swappable, Func<bool> trust)
        : base(swappable, s => new SslStream(s, true, (s, c, ch, ss) => trust()))
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
