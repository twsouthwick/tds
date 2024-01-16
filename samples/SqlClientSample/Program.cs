using Microsoft.Data.SqlClient;

using var conn = new SqlConnection(args[0]);

await conn.OpenAsync();

SqlCommand cmd = conn.CreateCommand();
cmd.CommandText = "SELECT *";
cmd.ExecuteNonQuery();
