using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;
using System.IO.Pipelines;
using System.Net.Security;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Protocols.Tds;

internal sealed class SslDuplexPipeFeature : IDuplexPipe, ISslFeature, ITdsConnectionFeature, IDisposable
{
    private readonly TdsConnectionContext _context;
    private readonly TdsPacketAdapter _original;
    private readonly SslDuplexAdapter _ssl;

    private IDuplexPipe _current;

    public SslDuplexPipeFeature(TdsConnectionContext context, TdsPacketAdapter original)
    {
        _context = context;
        _original = original;
        _ssl = new(original);
        _current = original;
    }

    PipeReader IDuplexPipe.Input => _current.Input;

    PipeWriter IDuplexPipe.Output => _current.Output;

    public bool IsEnabled => ReferenceEquals(_current, _ssl) && _ssl.Stream.IsAuthenticated;

    ValueTask ISslFeature.DisableAsync()
    {
        _current = _original;
        return ValueTask.CompletedTask;
    }

    async ValueTask ISslFeature.EnableAsync()
    {
        if (!_ssl.Stream.IsAuthenticated)
        {
            await _current.Output.FlushAsync(_context.Aborted);

            var options = new SslClientAuthenticationOptions
            {
                TargetHost = "localhost",
                RemoteCertificateValidationCallback = AllowAll,
            };

            _original.Type = TdsType.PreLogin;

            await _ssl.Stream.AuthenticateAsClientAsync(options, _context.Aborted);
        }

        _current = _ssl;
    }

    private static bool AllowAll(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        => true;

    public void Dispose()
    {
        _ssl.Dispose();
    }

    async ValueTask ITdsConnectionFeature.WritePacket(ITdsPacket packet)
    {
        _original.Type = packet.Type;
        packet.Write(_context, _current.Output);
        await _current.Output.FlushAsync(_context.Aborted);
    }

    async ValueTask ITdsConnectionFeature.ReadPacketAsync(ITdsPacket packet)
    {
        var packetBytes = await _current.Input.ReadAsync(_context.Aborted);

        packet.Read(_context, packetBytes.Buffer);

        _current.Input.AdvanceTo(packetBytes.Buffer.End);
    }

    private sealed class SslDuplexAdapter(IDuplexPipe duplexPipe) : DuplexPipeStreamAdapter<SslStream>(duplexPipe, s => new SslStream(s))
    {
    }
}
