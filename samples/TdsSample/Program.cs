using Microsoft.Protocols.Tds;
using Microsoft.Protocols.Tds.Packets;

var connections = TdsConnectionPool.Create(builder => builder
    .UseSockets()
    .UseSqlAuthentication()
    .UseDefaultPacketProcessor()
    .UseAuthentication());

await using var connection = await connections.OpenAsync(args[0]);

await connection.ExecuteAsync(args[1]);
