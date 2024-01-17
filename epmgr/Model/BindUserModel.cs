using System;
using System.ComponentModel.DataAnnotations;

namespace epmgr.Model
{
    public class BindUserModel
    {
        [Required]
        public string Caller { get; set; }

        [Required]
        public string Token { get; set; }
    }
}
