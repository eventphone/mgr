using System;
using System.ComponentModel.DataAnnotations;

namespace epmgr.Model
{
    public class CreateUserModel
    {
        [Required]
        [Display(Name = "User name", Prompt = "User name")]
        [StringLength(255)]
        public string Username { get; set; }
            
        [Required]
        [Display(Name = "Password", Prompt = "Password")]
        public string Password { get; set; }

        [Required]
        [Display(Name="Password", Prompt = "Repeat password")]
        public string Password2 { get; set; }
    }
}
