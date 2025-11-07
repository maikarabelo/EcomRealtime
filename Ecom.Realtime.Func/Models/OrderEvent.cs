public sealed record OrderEvent(
    string OrderId,
    string CustomerId,
    decimal Amount,
    string Currency,
    string Status,
    DateTime EventEnqueuedUtc
);
