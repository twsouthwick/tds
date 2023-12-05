using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;
using System.Security.Cryptography;

namespace Microsoft.Protocols.Tds;

public class TdsConnectionContext
{
    private FeatureReference<ITdsConnectionFeature> _connection = FeatureReference<ITdsConnectionFeature>.Default;

    public IFeatureCollection Features { get; } = new FeatureCollection();

    public CancellationToken Aborted => Features.GetRequiredFeature<IAbortFeature>().Token;

    public Guid TraceId { get; } = Guid.NewGuid();

    public void Abort() => Features.GetRequiredFeature<IAbortFeature>().Abort();

    public ValueTask WritePacketAsync(ITdsPacket packet)
    {
        var feature = _connection.Fetch(Features) ?? throw new InvalidOperationException("No ITdsConnectionFeature available");

        return feature.WritePacket(packet);
    }

    public ValueTask<ITdsPacket> ReadPacketAsync()
    {
        var feature = _connection.Fetch(Features) ?? throw new InvalidOperationException("No ITdsConnectionFeature available");

        return feature.ReadPacketAsync();
    }

    internal ReadOnlySpan<byte> GetNonce()
    {
        if (Features.Get<Nonce>() is not { } feature)
        {
            feature = new();
            Features.Set(feature);
        }

        return feature.Value;
    }

    private sealed class Nonce
    {
        private byte[] _value;

        public Nonce()
        {
            _value = new byte[32];
            RandomNumberGenerator.Fill(_value);
        }

        public ReadOnlySpan<byte> Value => _value;
    }
}


public delegate ValueTask TdsConnectionDelegate(TdsConnectionContext context);
