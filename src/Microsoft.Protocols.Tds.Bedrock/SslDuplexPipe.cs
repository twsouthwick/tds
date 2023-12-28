﻿using System.IO.Pipelines;
using System.Net.Security;

namespace Microsoft.Protocols.Tds;

internal sealed class SslDuplexPipe : DuplexPipeStreamAdapter<SslStream>
{
    public SslDuplexPipe(IDuplexPipe transport, StreamPipeReaderOptions readerOptions, StreamPipeWriterOptions writerOptions)
        : this(transport, readerOptions, writerOptions, s => new SslStream(s))
    {
    }

    public SslDuplexPipe(IDuplexPipe transport, StreamPipeReaderOptions readerOptions, StreamPipeWriterOptions writerOptions, Func<Stream, SslStream> factory) :
        base(transport, readerOptions, writerOptions, factory)
    {
    }
}
