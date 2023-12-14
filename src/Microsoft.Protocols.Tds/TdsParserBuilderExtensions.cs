namespace Microsoft.Protocols.Tds;

public static class TdsParserBuilderExtensions
{
    public static ITdsConnectionBuilder Use(this ITdsConnectionBuilder builder, Func<TdsConnectionContext, TdsConnectionDelegate, ValueTask> middleware)
        => builder.Use(next => context => middleware(context, next));
}
