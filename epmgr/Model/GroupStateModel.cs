using System.ComponentModel.DataAnnotations;

namespace epmgr.Model
{
    public class GroupStateModel
    {
        [Required]
        public string Group { get; set; }
        
        [Required]
        public string Member { get; set; }
    }
}