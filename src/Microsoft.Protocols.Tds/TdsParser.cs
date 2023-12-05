using Microsoft.Protocols.Tds.Features;
using System.Net;

namespace Microsoft.Protocols.Tds;

public sealed class TdsParser(TdsConnectionDelegate tdsConnection) : IConnectionStringFeature
{
    public async ValueTask ExecuteAsync()
    {
        var context = new TdsConnectionContext();

        context.Features.Set<IConnectionStringFeature>(this);

        await tdsConnection(context);
    }

    string IConnectionStringFeature.ConnectionString => Host ?? string.Empty;

    public string? Host { get; set; }

    public int Port { get; set; } = 1433;

    public IPAddress? IPAddress { get; set; }
}
