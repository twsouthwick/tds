using System.Net;

namespace Microsoft.Protocols.Tds.Features;

public interface IConnectionFeature
{
    string HostName { get; set; }

    EndPoint? Endpoint { get; set; }

    string Database { get; set; }

    bool TrustServerCertificate { get; set; }

    bool IsEncrypted { get; set; }
}
