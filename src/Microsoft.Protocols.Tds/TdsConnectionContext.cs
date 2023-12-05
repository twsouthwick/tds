using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;

namespace Microsoft.Protocols.Tds;

public class TdsConnectionContext
{
    private FeatureReference<ITdsConnectionFeature> _connection = FeatureReference<ITdsConnectionFeature>.Default;

    public IFeatureCollection Features { get; } = new FeatureCollection();

    public CancellationToken Aborted => Features.GetRequiredFeature<IAbortFeature>().Token;

    public Guid? TraceId { get; } = Guid.NewGuid();

    public void Abort() => Features.GetRequiredFeature<IAbortFeature>().Abort();

    public ValueTask WritePacketAsync(ITdsPacket packet)
    {
        var feature = _connection.Fetch(Features) ?? throw new InvalidOperationException("No ITdsConnectionFeature available");

        return feature.WritePacket(packet);
    }

    public ValueTask<TdsResponsePacket> ReadPacketAsync()
    {
        var feature = _connection.Fetch(Features) ?? throw new InvalidOperationException("No ITdsConnectionFeature available");

        return feature.ReadPacketAsync();
    }

    public ValueTask SendPacketAsync(TdsType type)
    {
        var packet = Features.GetRequiredFeature<IPacketCollectionFeature>().Get(type) ?? throw new KeyNotFoundException();
        return WritePacketAsync(packet);
    }
}


public delegate ValueTask TdsConnectionDelegate(TdsConnectionContext context);
