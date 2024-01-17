using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace epmgr.Pages.Yate
{
    public class StatusModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public StatusModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult OnGet()
        {
            var connectionString = _configuration.GetConnectionString("Redis");
            if (String.IsNullOrEmpty(connectionString))
                return NotFound();
            return Page();
        }
    }
}