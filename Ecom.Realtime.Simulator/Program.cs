using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System.Text;
using System.Text.Json;

var conn = Environment.GetEnvironmentVariable("EVENT_HUB_CONN");
if (string.IsNullOrWhiteSpace(conn))
{
    Console.WriteLine("Set environment variable EVENT_HUB_CONN to your Event Hub Send connection string.");
    return;
}

var producer = new EventHubProducerClient(conn);
var rnd = new Random();

Console.WriteLine("Starting simulator... Ctrl+C to stop.");

while (true)
{
    var burst = rnd.Next(200, 2000);

    using EventDataBatch batch = await producer.CreateBatchAsync();
    for (int i = 0; i < burst; i++)
    {
        var evt = new
        {
            OrderId = Guid.NewGuid().ToString("N")[..12],
            CustomerId = Guid.NewGuid().ToString("N")[..8],
            Amount = Math.Round((decimal)(rnd.NextDouble() * 1500.0 + 5.0), 2),
            Currency = new[] { "USD", "EUR", "ZAR", "GBP" }[rnd.Next(4)],
            Status = new[] { "Placed", "Paid", "Shipped" }[rnd.Next(3)],
            EventEnqueuedUtc = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(evt);
        if (!batch.TryAdd(new EventData(Encoding.UTF8.GetBytes(json))))
        {
            await producer.SendAsync(batch);
            using var next = await producer.CreateBatchAsync();
            next.TryAdd(new EventData(Encoding.UTF8.GetBytes(json)));
            await producer.SendAsync(next);
        }
    }

    await producer.SendAsync(batch);
    await Task.Delay(rnd.Next(50, 200));
}
