using Microsoft.Protocols.Tds.Features;
using System.Net;

namespace Microsoft.Protocols.Tds;

public static class TdsFormatter
{
    private sealed class PacketFormatter : IFormattable
    {
        public string ToString(string format, IFormatProvider formatProvider)
        {
            throw new NotImplementedException();
        }
    }
}
public sealed class TdsParser(TdsConnectionDelegate tdsConnection, IServiceProvider services) : IConnectionStringFeature
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
