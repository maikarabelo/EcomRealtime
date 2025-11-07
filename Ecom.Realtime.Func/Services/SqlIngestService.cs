using System.Data;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

public sealed class SqlIngestService
{
    private readonly string _connStr;
    public SqlIngestService(IConfiguration cfg)
        => _connStr = cfg.GetConnectionString("SqlServer")
           ?? throw new InvalidOperationException("Missing connection string 'SqlServer'.");

    public async Task BulkInsertAsync(IEnumerable<OrderEvent> events, int partitionId, CancellationToken ct)
    {
        var table = new DataTable();
        table.Columns.Add("OrderId", typeof(string));
        table.Columns.Add("CustomerId", typeof(string));
        table.Columns.Add("Amount", typeof(decimal));
        table.Columns.Add("Currency", typeof(string));
        table.Columns.Add("Status", typeof(string));
        table.Columns.Add("EventEnqueuedUtc", typeof(DateTime));
        table.Columns.Add("PartitionId", typeof(int));
        table.Columns.Add("RawEvent", typeof(string));

        foreach (var e in events)
        {
            table.Rows.Add(e.OrderId, e.CustomerId, e.Amount, e.Currency, e.Status,
                e.EventEnqueuedUtc == default ? DateTime.UtcNow : e.EventEnqueuedUtc,
                partitionId, JsonSerializer.Serialize(e));
        }

        await using var con = new SqlConnection(_connStr);
        await con.OpenAsync(ct);

        using var bulk = new SqlBulkCopy(con)
        {
            DestinationTableName = "dbo.OrderEvents_Processed",
            BatchSize = 5000
        };
        bulk.ColumnMappings.Add("OrderId", "OrderId");
        bulk.ColumnMappings.Add("CustomerId", "CustomerId");
        bulk.ColumnMappings.Add("Amount", "Amount");
        bulk.ColumnMappings.Add("Currency", "Currency");
        bulk.ColumnMappings.Add("Status", "Status");
        bulk.ColumnMappings.Add("EventEnqueuedUtc", "EventEnqueuedUtc");
        bulk.ColumnMappings.Add("PartitionId", "PartitionId");
        bulk.ColumnMappings.Add("RawEvent", "RawEvent");

        await bulk.WriteToServerAsync(table, ct);
    }
}
