using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace epmgr.Data.ywsd
{
    public class Yate
    {
        public const string AppId = "APP";
        public const string SipId = "SIP";
        public const string DectId = "DECT";
        public const string PremiumId = "PREMIUM";
        public const string GsmId = "GSM";

        [Column("id")]
        public int Id { get; set; }

        [StringLength(256)]
        [Column("hostname")]
        public string Hostname { get; set; }

        [StringLength(32)]
        [Column("guru3_identifier")]
        public string Guru3Identifier { get; set; }
    }
}