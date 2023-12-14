using System.Net;

namespace Microsoft.Protocols.Tds.Features;

public interface IConnectionStringFeature
{
    string ConnectionString { get; }

    EndPoint Endpoint { get; }

    string Database { get; }
}
