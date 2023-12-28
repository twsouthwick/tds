using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Protocols.Tds;
using Microsoft.Protocols.Tds.Packets;
using System.Net;

var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddFilter(_ => true);
});

using var provider = services.BuildServiceProvider();

var tds = TdsConnectionBuilder.Create(provider)
    .UseDefaultPacketProcessor()
    //.UseSslStream()
    .UseBedrock()
    .UseAuthentication()
    .Build();

var parser = new TdsParser(tds)
{
    Endpoint = new DnsEndPoint("localhost", 1433),
    Database = "tempdb",
    AppName = "app",
    ServerName = "",
    UserName = "SA",
    Password = "<YourStrong@Passw0rd>",
};

await parser.ExecuteAsync();
