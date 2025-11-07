using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public sealed class EventHubIngestFunction
{
    private readonly SqlIngestService _ingest;
    private readonly ILogger<EventHubIngestFunction> _log;

    public EventHubIngestFunction(SqlIngestService ingest, ILogger<EventHubIngestFunction> log)
    {
        _ingest = ingest;
        _log = log;
    }

    [Function("EventHubIngest")]
    public async Task RunAsync(
        [EventHubTrigger("%EVENT_HUB_NAME%", Connection = "EVENT_HUB_CONN", ConsumerGroup = "%EVENT_HUB_CONSUMER_GROUP%")]
        string[] events,
        int partitionId)
    {
        try
        {
            var parsed = events.Select(payload =>
            {
                var e = JsonSerializer.Deserialize<OrderEvent>(payload);
                if (e is null) throw new InvalidOperationException("Invalid event payload.");
                return e;
            }).ToArray();

            await _ingest.BulkInsertAsync(parsed, partitionId, CancellationToken.None);
            _log.LogInformation("Inserted {Count} events (partition {PartitionId})", parsed.Length, partitionId);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Processing failed for partition {PartitionId}", partitionId);
            throw;
        }
    }
}
