using System;
using System.ComponentModel.DataAnnotations;

namespace epmgr.Data
{
    public class MgrExtension
    {
        [Key]
        public int Id { get; set; }

        [StringLength(32)]
        public string Extension { get; set; }

        public MgrExtensionType Type { get; set; }

        [StringLength(64)]
        public string Name { get; set; }

        [StringLength(32)]
        public string Password { get; set; }

        [StringLength(32)]
        public string Token { get; set; }

        [StringLength(5)]
        public string Language { get; set; }

        public bool UseEncryption { get; set; }

        public bool DeleteAfterResync { get; set; }
    }
}
