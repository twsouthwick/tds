namespace Microsoft.Protocols.Tds.Features;

public interface ISqlUserAuthenticationFeature
{
    string HostName { get; }

    string UserName { get; }

    string Password { get; }
}

