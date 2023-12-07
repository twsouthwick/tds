namespace Microsoft.Protocols.Tds.Features;

public interface IEnvironmentFeature
{
    Version Version { get; }

    int ThreadId { get; }
}
