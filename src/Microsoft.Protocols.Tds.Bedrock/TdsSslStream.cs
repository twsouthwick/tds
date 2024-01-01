using Microsoft.Protocols.Tds.Packets;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Protocols.Tds;

internal sealed class SslDuplexAdapter(IDuplexPipe duplexPipe) : DuplexPipeStreamAdapter<SslStream>(duplexPipe, s => new SslStream(s))
{
    private bool _isAuthenticating;

    public override void Write(byte[] buffer, int offset, int count)
        => Write(buffer.AsSpan(offset, count));

    public async ValueTask AuthenticateAsync(SslClientAuthenticationOptions options, CancellationToken token)
    {
        _isAuthenticating = true;
        await Stream.AuthenticateAsClientAsync(options, token);
        _isAuthenticating = false;
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (_isAuthenticating)
        {
            _isAuthenticating = false;
            var b = new ArrayBufferWriter<byte>();
            b.WriteHeader(TdsType.PreLogin, (short)buffer.Length);
            b.Write(buffer);
            base.Write(b.WrittenSpan);
            _isAuthenticating = true;
        }
        else
        {
            base.Write(buffer);
        }
    }

    public override Task WriteAsync(byte[]? buffer, int offset, int count, CancellationToken cancellationToken)
        => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_isAuthenticating)
        {
            var b = new ArrayBufferWriter<byte>();
            b.WriteHeader(TdsType.PreLogin, (short)buffer.Length);
            b.Write(buffer.Span);
            _isAuthenticating = false;
            await base.WriteAsync(b.WrittenMemory, cancellationToken);
            _isAuthenticating = true;
        }
        else
        {
            await base.WriteAsync(buffer, cancellationToken);
        }
    }

    protected override async ValueTask<int> ReadAsyncInternal(Memory<byte> destination, CancellationToken cancellationToken)
    {
        if (destination.Length == 0)
        {
            return 0;
        }

        if (_isAuthenticating)
        {
            var header = destination.Slice(0, 8);

            _isAuthenticating = false;
            await ReadExactlyAsync(header, cancellationToken);

            var parsed = MemoryMarshal.Read<TdsOptionsHeader>(header.Span);
            //destination = destination.Slice(0, parsed.GetLength());

            var length = await base.ReadAsyncInternal(destination, cancellationToken);
            _isAuthenticating = true;

            return length;
        }
        else
        {
            return await base.ReadAsyncInternal(destination, cancellationToken);
        }
    }

    private int ReadHeader()
    {
        Span<byte> b = new byte[8];
        ReadExactly(b);

        return MemoryMarshal.Read<TdsOptionsHeader>(b).GetLength();
    }
}
