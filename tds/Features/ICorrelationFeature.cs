namespace Microsoft.Protocols.Tds.Features;

public interface ICorrelationFeature
{
    Guid ActivityId { get; }

    uint SequenceId { get; }
}
