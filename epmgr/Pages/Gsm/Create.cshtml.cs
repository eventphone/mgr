using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Gsm;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace epmgr.Pages.Gsm
{
    public class CreateModel : PageModel
    {
        private readonly IGsmClient _gsmClient;

        [BindProperty]
        [Required]
        public string Imsi { get; set; }

        [BindProperty]
        [Required]
        public string Name { get; set; }

        public CreateModel(IGsmClient gsmClient)
        {
            _gsmClient = gsmClient;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return Page();
            await _gsmClient.CreateSubscriber(Imsi, Name, cancellationToken);
            return RedirectToPage("Index");
        }
    }
}