using Microsoft.Protocols.Tds;

var connections = TdsConnectionPool.CreateDefault();

await using var connection = await connections.OpenAsync(args[0]);

Console.WriteLine("Test");
//await connection.ExecuteAsync(args[1]);
