using System.ComponentModel.DataAnnotations;

namespace epmgr.Model
{
    public class LoginUserModel
    {
        [Required]
        [Display(Name = "User name", Prompt = "User name")]
        [StringLength(255)]
        public string Username { get; set; }
            
        [Required]
        [Display(Name = "Password", Prompt = "Password")]
        public string Password { get; set; }

        [Display(Name="Keep me logged in")]            
        public bool Persistent { get; set; }
    }
}