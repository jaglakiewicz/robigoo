using Microsoft.Data.Sqlite;
var conn = new SqliteConnection("Data Source=app_v2.db");
conn.Open();
var cmd = conn.CreateCommand();
cmd.CommandText = "UPDATE UserSessions SET IsActive = 0, InvalidatedAt = datetime('now'), InvalidationReason = 'Manual cleanup' WHERE IsActive = 1;";
var rows = cmd.ExecuteNonQuery();
Console.WriteLine($"Updated {rows} sessions");
conn.Close();
