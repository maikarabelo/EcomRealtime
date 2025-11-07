using Microsoft.AspNetCore.Mvc;
using Ecom.Realtime.Mvc.Data;
using Ecom.Realtime.Mvc.Models;

namespace Ecom.Realtime.Mvc.Controllers;

public class DashboardController : Controller
{
    private readonly ReadRepository _repo;
    public DashboardController(ReadRepository repo) => _repo = repo;

    public async Task<IActionResult> Index()
    {
        var (count, total) = await _repo.GetLastMinuteAsync();
        var series = await _repo.GetSeriesAsync(60);
        var model = new DashboardViewModel
        {
            LastMinuteCount = count,
            LastMinuteTotal = total,
            Series = series
        };
        return View(model);
    }
}
