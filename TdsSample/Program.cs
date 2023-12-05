﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Protocols.Tds;
using Microsoft.Protocols.Tds.Packets;

//var str = new SqlConnectionStringBuilder
//{
//    Authentication = SqlAuthenticationMethod.SqlPassword,
//    UserID = "sa",
//    Password = "<YourStrong!Passw0rd>",
//    DataSource = "127.0.0.1",
//    TrustServerCertificate = true
//}.ToString();

//using var conn = new SqlConnection(str);
//await conn.OpenAsync();

var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddFilter(_ => true);
});

using var provider = services.BuildServiceProvider();

var builder = TdsConnectionBuilder.Create(provider)
    .UseHostResolution()
    .UseBedrock()
    .Use(async (ctx, next) =>
    {
        await ctx.WritePacketAsync(Packets.PreLogin.Packet);
        await ctx.ReadPacketAsync();
    })
    .Build();

var parser = new TdsParser(builder)
{
    Host = "127.0.0.1"
};

await parser.ExecuteAsync();
