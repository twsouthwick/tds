namespace Microsoft.Protocols.Tds.Features;

public interface ISslFeature
{
    bool IsEnabled { get; }

    ValueTask EnableAsync();

    ValueTask DisableAsync();
}
