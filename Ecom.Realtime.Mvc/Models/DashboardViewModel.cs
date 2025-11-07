namespace Ecom.Realtime.Mvc.Models;

public sealed class DashboardViewModel
{
    public int LastMinuteCount { get; set; }
    public decimal LastMinuteTotal { get; set; }
    public IReadOnlyList<TimeBucket> Series { get; set; } = Array.Empty<TimeBucket>();
}
