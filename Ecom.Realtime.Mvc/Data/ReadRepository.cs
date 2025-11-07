using Microsoft.Data.SqlClient;
using System.Data;
using Ecom.Realtime.Mvc.Models;

namespace Ecom.Realtime.Mvc.Data;

public sealed class ReadRepository
{
    private readonly string _connStr;
    public ReadRepository(IConfiguration cfg) => _connStr = cfg.GetConnectionString("SqlServer")!;

    public async Task<(int Count, decimal Total)> GetLastMinuteAsync()
    {
        const string sql = @"
            SELECT COUNT(*) as Cnt, COALESCE(SUM(Amount),0) as Tot
            FROM dbo.OrderEvents_Processed
            WHERE ProcessedUtc >= DATEADD(minute, -1, SYSUTCDATETIME());";

        await using var con = new SqlConnection(_connStr);
        await con.OpenAsync();
        await using var cmd = new SqlCommand(sql, con);
        await using var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
        if (await rdr.ReadAsync())
            return (rdr.GetInt32(0), rdr.GetDecimal(1));
        return (0, 0m);
    }

    public async Task<IReadOnlyList<TimeBucket>> GetSeriesAsync(int minutes = 60)
    {
        const string sql = @"
            SELECT DATEADD(minute, DATEDIFF(minute, 0, ProcessedUtc), 0) AS Bucket,
                   SUM(Amount) AS Total
            FROM dbo.OrderEvents_Processed
            WHERE ProcessedUtc >= DATEADD(minute, -@mins, SYSUTCDATETIME())
            GROUP BY DATEADD(minute, DATEDIFF(minute, 0, ProcessedUtc), 0)
            ORDER BY Bucket;";

        var result = new List<TimeBucket>();
        await using var con = new SqlConnection(_connStr);
        await con.OpenAsync();
        await using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@mins", minutes);
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
        {
            result.Add(new TimeBucket
            {
                Bucket = rdr.GetDateTime(0),
                Total  = rdr.GetDecimal(1)
            });
        }
        return result;
    }
}
