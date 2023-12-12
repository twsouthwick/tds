namespace Microsoft.Protocols.Tds.Features;

public interface IAuthenticationFeature
{
    bool IsAuthenticated { get; }

    ValueTask AuthenticateAsync();
}

