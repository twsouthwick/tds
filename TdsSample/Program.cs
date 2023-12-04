using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds;
using Microsoft.Protocols.Tds.Features;

var builder = TdsConnectionBuilder.Create()
    .UseBedrock("CPC-tasou-I0BEC")
    .Use(async (ctx, next) =>
    {
        var pipe = ctx.Features.GetRequiredFeature<ITdsConnectionFeature>().Pipe;

        var result = await pipe.Input.ReadAsync(ctx.Aborted);
    })
    .Build();

var parser = new TdsParser(builder);

await parser.ExecuteAsync("some string");
