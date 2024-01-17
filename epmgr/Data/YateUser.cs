using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace epmgr.Data
{
    public class YateRegistration
    {
        [StringLength(32)]
        [Column("username")]
        public string Username { get; set; }

        [Column("location")]
        [StringLength(1024)]
        public string Location { get; set; }

        [Column("oconnection_id")]
        [StringLength(1024)]
        public string OConnectionId { get; set; }

        [Column("expires")]
        public DateTime? Expires { get; set; }
    }

    public abstract class YateUser
    {
        protected YateUser()
        {
            CallWaiting = true;
            StaticTarget = String.Empty;
        }

        [Key]
        [StringLength(32)]
        [Column("username")]
        public string Username { get; set; }

        [Column("displayname")]
        [StringLength(64)]
        public string DisplayName { get; set; }

        [Column("password")]
        [StringLength(128)]
        public string Password { get; set; }

        [Column("inuse")]
        public int InUse { get; set; }

        [Column("type")]
        [StringLength(20)]
        public string Type { get; set; }

        [NotMapped]
        public abstract MgrExtensionType UserType { get; }

        [Column("trunk")]
        public bool IsTrunk { get; set; }

        [Column("call_waiting")]
        public bool CallWaiting { get; set; }

        [Column("static_target")]
        [StringLength(1024)]
        public string StaticTarget { get; set; }
    }
}