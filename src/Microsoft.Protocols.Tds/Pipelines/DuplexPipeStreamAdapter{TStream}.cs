﻿using System.IO.Pipelines;

namespace Microsoft.Protocols.Tds;

/// <summary>
/// A helper for wrapping a Stream decorator from an <see cref="IDuplexPipe"/>.
/// </summary>
/// <typeparam name="TStream"></typeparam>
internal class DuplexPipeStreamAdapter<TStream> : DuplexPipeStream, IDuplexPipe where TStream : Stream
{
    private bool _disposed;
    private readonly object _disposeLock = new object();

    public DuplexPipeStreamAdapter(IDuplexPipe duplexPipe, Func<Stream, TStream> createStream) :
        this(duplexPipe, new StreamPipeReaderOptions(leaveOpen: true), new StreamPipeWriterOptions(leaveOpen: true), createStream)
    {
    }

    public DuplexPipeStreamAdapter(IDuplexPipe duplexPipe, StreamPipeReaderOptions readerOptions, StreamPipeWriterOptions writerOptions, Func<Stream, TStream> createStream) :
        base(duplexPipe)
    {
        var stream = createStream(this);
        Stream = stream;
        Input = PipeReader.Create(stream, readerOptions);
        Output = PipeWriter.Create(stream, writerOptions);
    }

    public TStream Stream { get; }

    public PipeReader Input { get; }

    public PipeWriter Output { get; }

#if NET
    public override async ValueTask DisposeAsync()
    {
        lock (_disposeLock)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
        }

        await Input.CompleteAsync();
        await Output.CompleteAsync();
    }
#endif

    protected override void Dispose(bool disposing)
    {
        lock (_disposeLock)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
        }

        Input.Complete();
        Output.Complete();
    }
}

