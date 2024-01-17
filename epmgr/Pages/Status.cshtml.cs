using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace epmgr.Pages
{
    public class StatusModel : PageModel
    {
        private readonly StatsService _statsService;

        public StatusModel(StatsService statsService)
        {
            _statsService = statsService;
        }

        public void OnGet()
        {
        }

        public Task<IActionResult> OnPostUpAsync(string type, CancellationToken cancellationToken)
        {
            return SetStatusAsync(type, 1, cancellationToken);
        }

        public Task<IActionResult> OnPostDownAsync(string type, CancellationToken cancellationToken)
        {
            return SetStatusAsync(type, -1, cancellationToken);
        }

        public Task<IActionResult> OnPostUnstableAsync(string type, CancellationToken cancellationToken)
        {
            return SetStatusAsync(type, 0, cancellationToken);
        }

        private async Task<IActionResult> SetStatusAsync(string type, int value, CancellationToken cancellationToken)
        {
            await _statsService.SendMetricValuesAsync(new Dictionary<string, double> {{$"poc.status.{type}", value}}, cancellationToken);
            return RedirectToPage();
        }
    }
}