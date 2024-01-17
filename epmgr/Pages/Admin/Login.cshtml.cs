using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace epmgr.Pages.Admin
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly MgrDbContext _context;

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public bool Persistent { get; set; }

        public LoginModel(MgrDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet(string returnUrl)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToPage("/Index");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            var mgrUser = await _context.Users.FirstOrDefaultAsync(x => x.Username == Username, cancellationToken);
            if (mgrUser == null)
            {
                ModelState.AddModelError("username", "User not found");
                return Page();
            }

            var passwordHasher = new PasswordHasher<object>();
            var result = passwordHasher.VerifyHashedPassword(null, mgrUser.PasswordHash, Password);
            switch (result)
            {
                case PasswordVerificationResult.Failed:
                    ModelState.AddModelError("password", "Invalid password");
                    return Page();
                case PasswordVerificationResult.SuccessRehashNeeded:
                    mgrUser.PasswordHash = passwordHasher.HashPassword(null, Password);
                    break;
            }
            mgrUser.LastLogon = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            await HttpContext.SignInAsync(
                new ClaimsPrincipal(new ClaimsIdentity(new[] {new Claim(ClaimTypes.Name, Username), new Claim(ClaimTypes.NameIdentifier, mgrUser.Id.ToString()), },
                    CookieAuthenticationDefaults.AuthenticationScheme)),new AuthenticationProperties{IsPersistent = Persistent});
            if (!String.IsNullOrEmpty(returnUrl))
                return LocalRedirect(returnUrl);
            return RedirectToPage("/Index");
        }
    }
}