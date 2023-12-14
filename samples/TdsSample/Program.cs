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
    .UseAuthentication()
    .Build();

var parser = new TdsParser(tds)
{
    Host = "127.0.0.1",
    Database = "test",
    AppName = "app",
    ServerName = "server",
    UserName = "SA",
    Password = "<YourStrong@Passw0rd>",
};

await parser.ExecuteAsync();
