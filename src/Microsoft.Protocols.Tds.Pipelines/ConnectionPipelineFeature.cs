using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;
using System.IO.Pipelines;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Protocols.Tds;

internal sealed class ConnectionPipelineFeature : IDuplexPipe, ISslFeature, ITdsConnectionFeature, IDisposable
{
    private readonly IDuplexPipe _original;
    private readonly TdsConnectionContext _context;
    private readonly TdsPacketAdapter _tds;
    private readonly SslDuplexPipe _ssl;

    private IDuplexPipe _current;

    public ConnectionPipelineFeature(TdsConnectionContext context, IDuplexPipe original)
    {
        _context = context;
        _original = original;
        _tds = new(original);
        _ssl = new(_tds);
        _current = _tds;
    }

    PipeReader IDuplexPipe.Input => _current.Input;

    PipeWriter IDuplexPipe.Output => _current.Output;

    public bool IsEnabled => ReferenceEquals(_current, _ssl) && _ssl.Stream.IsAuthenticated;

    ValueTask ISslFeature.DisableAsync()
    {
        RemoveSsl();

        return ValueTask.CompletedTask;
    }

    async ValueTask ISslFeature.EnableAsync()
    {
        if (!_ssl.Stream.IsAuthenticated)
        {
            var options = new SslClientAuthenticationOptions
            {
                TargetHost = "localhost",
                RemoteCertificateValidationCallback = AllowAll,
            };

            SetSslAsInner();

            _tds.Type = TdsType.PreLogin;

            await _ssl.Stream.AuthenticateAsClientAsync(options, _context.Aborted);
        }

        SetSslAsOuter();
    }

    private void RemoveSsl()
    {
        _current = _tds;
        _tds.InnerPipe = _original;
    }

    private void SetSslAsOuter()
    {
        _current = _tds;
        _tds.InnerPipe = _ssl;
        _ssl.InnerPipe = _original;
    }

    private void SetSslAsInner()
    {
        _current = _ssl;
        _ssl.InnerPipe = _tds;
        _tds.InnerPipe = _original;
    }

    private static bool AllowAll(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        => true;

    public void Dispose()
    {
        _ssl.Dispose();
    }

    async ValueTask ITdsConnectionFeature.WritePacket(ITdsPacket packet)
    {
        _tds.Type = packet.Type;
        packet.Write(_context, _current.Output);
        await _current.Output.FlushAsync(_context.Aborted);
    }

    async ValueTask ITdsConnectionFeature.ReadPacketAsync(ITdsPacket packet)
    {
        var packetBytes = await _current.Input.ReadAsync(_context.Aborted);

        if (packetBytes.IsCompleted)
        {
            throw new InvalidOperationException("Connection has completed");
        }

        packet.Read(_context, packetBytes.Buffer);

        _current.Input.AdvanceTo(packetBytes.Buffer.End);
    }
}
