namespace Microsoft.Protocols.Tds.Features;

public interface IAuthenticationFeature
{
    bool IsAuthenticated { get; }

    ValueTask AuthenticateAsync();
}

internal interface IConnectionOpenFeature
{
    void Initialized();

    ValueTask WaitForInitializedAsync();

    ValueTask WaitForDisposeAsync();

    ValueTask DisposeAsync();

    bool IsOpened { get; }
}
