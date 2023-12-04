using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;

namespace Microsoft.Protocols.Tds;

public class TdsConnectionContext
{

    public IFeatureCollection Features { get; } = new FeatureCollection();

    public CancellationToken Aborted => Features.GetRequiredFeature<IAbortFeature>().Token;

    public void Abort() => Features.GetRequiredFeature<IAbortFeature>().Abort();
}


public delegate ValueTask TdsConnectionDelegate(TdsConnectionContext context);
