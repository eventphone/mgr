using System;
using System.ComponentModel.DataAnnotations;

namespace epmgr.Data
{
    public class MgrUser
    {
        public int Id { get; set; }

        [StringLength(64)]
        public string Username { get; set; }
        
        [StringLength(255)]
        public string PasswordHash { get; set; }

        public DateTimeOffset LastLogon { get; set; }
    }
}