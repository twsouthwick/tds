namespace Microsoft.Protocols.Tds;

public interface ITdsConnectionBuilder
{
    IServiceProvider Services { get; }

    ITdsConnectionBuilder Use(Func<TdsConnectionDelegate, TdsConnectionDelegate> middleware);

    ITdsConnectionBuilder New();

    IDictionary<string, object?> Properties { get; }

    TdsConnectionDelegate Build();
}
