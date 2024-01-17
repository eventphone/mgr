using System;
using System.Threading;
using System.Threading.Tasks;
using epmgr.Data;
using epmgr.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace epmgr.Pages.User
{
    public class PasswordModel : PageModel
    {
        private readonly MgrDbContext _context;

        [BindProperty]
        public ChangePasswordModel Password { get; set; }

        public PasswordModel(MgrDbContext context)
        {
            _context = context;
        }

        public void OnGet(int id)
        {
            Password = new ChangePasswordModel{Id = id};
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {            
            if (String.IsNullOrEmpty(Password.NewPassword))
            {
                ModelState.AddModelError(nameof(Password.NewPassword), "password is empty");
                return Page();
            }
            if (Password.NewPassword != Password.NewPassword2)
            {
                ModelState.AddModelError(nameof(Password.NewPassword2), "passwords do not match");
                return Page();
            }
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == Password.Id, cancellationToken);
            if (user != null)
            {
                var passwordHasher = new PasswordHasher<MgrUser>();
                if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, Password.OldPassword) != PasswordVerificationResult.Success)
                {
                    ModelState.AddModelError(nameof(Password.OldPassword), "invalid password");
                    return Page();
                }
                user.PasswordHash = passwordHasher.HashPassword(user, Password.NewPassword);
                await _context.SaveChangesAsync(cancellationToken);
            }
            return RedirectToPage("../User");
        }
    }
}