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

    public async ValueTask ReadPacketAsync(TdsType type)
    {
        var feature = _connection.Fetch(Features) ?? throw new InvalidOperationException("No ITdsConnectionFeature available");
        var packet = GetPacket(type);

        await feature.ReadPacketAsync(packet);
        await packet.RunAsync(this);
    }

    public ValueTask SendPacketAsync(TdsType type)
    {
        var packet = GetPacket(type);
        var feature = _connection.Fetch(Features) ?? throw new InvalidOperationException("No ITdsConnectionFeature available");

        return feature.WritePacket(packet);
    }

    private ITdsPacket GetPacket(TdsType type)
        => Features.GetRequiredFeature<IPacketCollectionFeature>().Get(type) ?? throw new InvalidOperationException();
}


public delegate ValueTask TdsConnectionDelegate(TdsConnectionContext context);
