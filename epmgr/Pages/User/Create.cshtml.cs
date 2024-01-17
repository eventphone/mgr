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
    public class CreateModel : PageModel
    {
        private readonly MgrDbContext _context;

        [BindProperty]
        public CreateUserModel NewUser { get; set; }

        public CreateModel(MgrDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
            NewUser = new CreateUserModel();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (String.IsNullOrEmpty(NewUser.Username))
                {
                    ModelState.AddModelError(nameof(NewUser.Username), "empty username");
                    throw new ArgumentException();
                }
                if (String.IsNullOrEmpty(NewUser.Password))
                {
                    ModelState.AddModelError(nameof(NewUser.Password), "empty password");
                    throw new ArgumentException();
                }
                if (NewUser.Password != NewUser.Password2)
                {
                    ModelState.AddModelError(nameof(NewUser.Password2), "passwords do not match");
                    throw new ArgumentException();
                }
                if (await _context.Users.AnyAsync(x => x.Username == NewUser.Username, cancellationToken: cancellationToken))
                {
                    ModelState.AddModelError(nameof(NewUser.Username), "username exists");
                    throw new ArgumentException();
                }
                
                var passwordHasher = new PasswordHasher<object>();
                var hash = passwordHasher.HashPassword(null, NewUser.Password);
                var mgrUser = new MgrUser
                {
                    Username = NewUser.Username,
                    PasswordHash = hash
                };
                _context.Users.Add(mgrUser);
                await _context.SaveChangesAsync(cancellationToken);

                return RedirectToPage("../User");
            }
            catch
            {
                return Page();
            }
        }
    }
}