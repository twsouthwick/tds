using Microsoft.Protocols.Tds;
using Microsoft.Protocols.Tds.Packets;

var pipeline = TdsConnectionBuilder.Create()
    .UseSockets()
    .UseSqlAuthentication()
    .UseDefaultPacketProcessor()
    .UseAuthentication()
    .Build();

var parser = new DefaultTdsConnectionContext(pipeline, args[0]);

await parser.ExecuteAsync(args[1]);
