using Microsoft.Protocols.Tds.Features;

namespace Microsoft.Protocols.Tds;

public sealed class TdsParser(TdsConnectionDelegate tdsConnection)
{
    public async ValueTask ExecuteAsync(string connectionString)
    {
        var context = new TdsConnectionContext();

        context.Features.Set<IConnectionStringFeature>(new RequestFeature(connectionString));

        await tdsConnection(context);
    }

    private sealed class RequestFeature(string connectionString) : IConnectionStringFeature
    {
        public string ConnectionString => connectionString;
    }
}
