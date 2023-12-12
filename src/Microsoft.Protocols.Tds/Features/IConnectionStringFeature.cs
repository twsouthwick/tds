using System.Net;

namespace Microsoft.Protocols.Tds.Features;

public interface IConnectionStringFeature
{
    string ConnectionString { get; }

    string? Host { get; }

    int Port { get; set; }

    IPAddress? IPAddress { get; set; }

    string Database { get; }
}
