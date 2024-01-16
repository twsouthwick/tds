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

var pipeline = TdsConnectionBuilder.Create(provider)
    .UseSockets()
    .UseSqlAuthentication()
    .UseDefaultPacketProcessor()
    .UseAuthentication()
    .Build();

var parser = new TdsConnection(pipeline, args[0]);

await parser.ExecuteAsync();
