using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;

namespace Microsoft.Protocols.Tds;

public class TdsConnectionContext
{
    private FeatureReference<ITdsConnectionFeature> _connection = FeatureReference<ITdsConnectionFeature>.Default;
    private FeatureReference<IPacketCollectionFeature> _collection = FeatureReference<IPacketCollectionFeature>.Default;

    public IFeatureCollection Features { get; } = new FeatureCollection();

    public CancellationToken Aborted => Features.GetRequiredFeature<IAbortFeature>().Token;

    public Guid? TraceId { get; } = Guid.NewGuid();

    public void Abort() => Features.GetRequiredFeature<IAbortFeature>().Abort();

    public async ValueTask ReadPacketAsync(TdsType type)
    {
        var feature = _connection.FetchRequired(Features);
        var packet = GetPacket(type);

        await feature.ReadPacketAsync(packet);
    }

    public async ValueTask SendPacketAsync(TdsType type)
    {
        var packet = GetPacket(type);
        var feature = _connection.FetchRequired(Features);

        await feature.WritePacket(packet);
    }

    public ITdsPacket GetPacket(TdsType type) => _collection.FetchRequired(Features).Get(type) ?? throw new InvalidOperationException($"No known packet with type '{type}'");
}

public delegate ValueTask TdsConnectionDelegate(TdsConnectionContext context);
