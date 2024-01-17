using Microsoft.Data.SqlClient;
using System.Data;

using var conn = new SqlConnection(args[0]);

await conn.OpenAsync();

SqlCommand cmd = conn.CreateCommand();
cmd.CommandText = args[1];

using var reader = await cmd.ExecuteReaderAsync(CancellationToken.None);

foreach (IDataRecord row in reader)
{
    Console.WriteLine(row.GetString(0));
}
