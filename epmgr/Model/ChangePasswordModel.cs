using System.ComponentModel.DataAnnotations;

namespace epmgr.Model
{
    public class ChangePasswordModel
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Old Password", Prompt = "Old Password")]
        public string OldPassword { get; set; }
        
        [Required]
        [Display(Name = "New Password", Prompt = "New Password")]
        public string NewPassword { get; set; }

        [Required]
        [Display(Name = "Repeat new Password", Prompt = "repeat")]
        public string NewPassword2 { get; set; }
    }
}