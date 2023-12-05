namespace Microsoft.Protocols.Tds.Features;

public interface IAbortFeature
{
    void Abort();

    CancellationToken Token { get; }
}
