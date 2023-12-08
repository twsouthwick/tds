using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Protocols.Tds;
using Microsoft.Protocols.Tds.Packets;

var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddFilter(_ => true);
});

using var provider = services.BuildServiceProvider();

var tds = TdsConnectionBuilder.Create(provider)
    .UseHostResolution()
    .UseDefaultPacketProcessor()
    .UseBedrock()
    .Use(async (ctx, next) =>
    {
        await ctx.SendPacketAsync(TdsType.PreLogin);
        await ctx.ReadPacketAsync(TdsType.PreLogin);
        await ctx.SendPacketAsync(TdsType.Login7);
        //await ctx.ReadPacketAsync(TdsType.Login7);
    })
    .Build();

var parser = new TdsParser(tds)
{
    Host = "127.0.0.1"
};

await parser.ExecuteAsync();
