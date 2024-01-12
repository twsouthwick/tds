using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Protocols.Tds;
using Microsoft.Protocols.Tds.Packets;
using System.Net;

if (args is not [{ }, { } engine, { } endpoint, { } username, { } password])
{
    Console.WriteLine("Invalid args: '" + string.Join("', '", args) + "'");
    return;
}

if (GetEndpoint(endpoint) is not { } parsedEndpoint)
{
    Console.WriteLine($"Unknown endpoint '{endpoint}'");
    return;
}

if (!Enum.TryParse<EngineType>(engine, ignoreCase: true, out var parsed))
{
    Console.WriteLine($"Unknown engine {engine}");
    return;
}

await (parsed switch
{
    EngineType.Bedrock => Bedrock(),
    EngineType.Sql => Sql(),
    _ => Unknown(engine),
});

async Task Sql()
{
    var connStr = new SqlConnectionStringBuilder
    {
        Encrypt = false,
        InitialCatalog = "tempdb",
        DataSource = endpoint,
        UserID = username,
        Password = password,
        TrustServerCertificate = true,
    };

    using var conn = new SqlConnection(connStr.ToString());

    await conn.OpenAsync();

    SqlCommand cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT *";
    cmd.ExecuteNonQuery();
}

async Task Bedrock()
{
    var services = new ServiceCollection();

    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.AddFilter(_ => true);
    });

    using var provider = services.BuildServiceProvider();

    var tds = TdsConnectionBuilder.Create(provider)
        .UseDefaultPacketProcessor()
        .UseBedrock()
        .UseAuthentication()
        .Build();

    var parser = new TdsParser(tds)
    {
        Endpoint = parsedEndpoint,
        Database = "tempdb",
        AppName = "Core Microsoft SqlClient Data Provider",
        ServerName = parsedEndpoint is DnsEndPoint dns ? dns.Host : "",
        UserName = username,
        Password = password,
    };

    await parser.ExecuteAsync();
}

Task Unknown(string engine)
{
    Console.WriteLine($"Unknown engine '{engine}'");
    return Task.CompletedTask;
}

EndPoint? GetEndpoint(string endpoint)
{
    if (IPEndPoint.TryParse(endpoint, out var ip))
    {
        return ip;
    }

    return endpoint.Split(':') switch
    {
    [{ } name] => new DnsEndPoint(name, 1433),
    [{ } name, { } port] when int.TryParse(port, out var parsed) => new DnsEndPoint(name, parsed),
        _ => null
    }; ;
}

enum EngineType
{
    Bedrock,
    Sql,
}

