using System.ComponentModel.DataAnnotations.Schema;

namespace epmgr.Data
{
    public class DectUser : YateUser
    {
        [Column("dect_displaymode")]
        public DectDisplayModus DisplayMode { get; set; }

        public override MgrExtensionType UserType { get; } = MgrExtensionType.DECT;
    }

    public class PremiumUser : YateUser
    {
        public override MgrExtensionType UserType { get; } = MgrExtensionType.PREMIUM;
    }

    public class SipUser : YateUser
    {
        public override MgrExtensionType UserType { get; } = MgrExtensionType.SIP;
    }

    public class AppUser : YateUser
    {
        public override MgrExtensionType UserType { get; } = MgrExtensionType.APP;
    }

    public class GsmUser : YateUser
    {
        public override MgrExtensionType UserType { get; } = MgrExtensionType.GSM;
    }
}