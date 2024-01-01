using System.Diagnostics;
using System.Net.Security;

namespace Microsoft.Protocols.Tds;

internal abstract class DelegatingSslStream(Stream stream) : SslStream(stream)
{
    public override bool CanRead => InnerStream.CanRead;

    public override bool CanSeek => InnerStream.CanSeek;

    public override bool CanWrite => InnerStream.CanWrite;

    public override long Length => InnerStream.Length;

    public override long Position
    {
        get => InnerStream.Position;
        set => InnerStream.Position = value;
    }

    public override int ReadTimeout
    {
        get => InnerStream.ReadTimeout;
        set => InnerStream.ReadTimeout = value;
    }

    public override bool CanTimeout => InnerStream.CanTimeout;

    public override int WriteTimeout
    {
        get => InnerStream.WriteTimeout;
        set => InnerStream.WriteTimeout = value;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            InnerStream.Dispose();
        }
        base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync() => InnerStream.DisposeAsync();

    public override long Seek(long offset, SeekOrigin origin) => InnerStream.Seek(offset, origin);

    public override int Read(byte[] buffer, int offset, int count) => InnerStream.Read(buffer, offset, count);

    public override int Read(Span<byte> buffer) => InnerStream.Read(buffer);

    public override int ReadByte() => InnerStream.ReadByte();

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => InnerStream.ReadAsync(buffer, offset, count, cancellationToken);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => InnerStream.ReadAsync(buffer, cancellationToken);

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => InnerStream.BeginRead(buffer, offset, count, callback, state);

    public override int EndRead(IAsyncResult asyncResult) => InnerStream.EndRead(asyncResult);

    public override void CopyTo(Stream destination, int bufferSize) => InnerStream.CopyTo(destination, bufferSize);

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => InnerStream.CopyToAsync(destination, bufferSize, cancellationToken);

    public override void Flush() => InnerStream.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) => InnerStream.FlushAsync(cancellationToken);

    public override void SetLength(long value) => InnerStream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count) => InnerStream.Write(buffer, offset, count);

    public override void Write(ReadOnlySpan<byte> buffer) => InnerStream.Write(buffer);

    public override void WriteByte(byte value) => InnerStream.WriteByte(value);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => InnerStream.WriteAsync(buffer, offset, count, cancellationToken);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => InnerStream.WriteAsync(buffer, cancellationToken);

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => InnerStream.BeginWrite(buffer, offset, count, callback, state);

    public override void EndWrite(IAsyncResult asyncResult) => InnerStream.EndWrite(asyncResult);
}
