using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace epmgr.Data.ywsd
{
    public class ForkRankMember
    {
        [Column("forkrank_id")]
        public int ForkRankId { get; set; }

        [ForeignKey(nameof(ForkRankId))]
        public ForkRank ForkRank { get; set; }

        [Column("extension_id")]
        public int ExtensionId { get; set; }

        [ForeignKey(nameof(ExtensionId))]
        public Extension Extension { get; set; }

        [Column("rankmember_type")]
        public ForkRankMemberType Type { get; set; }

        [Column("active")]
        public bool IsActive { get; set; }
    }

    public enum ForkRankMemberType
    {
        [PgName("DEFAULT")]
        Default,
        [PgName("AUXILIARY")]
        Auxiliary,
        [PgName("PERSISTENT")]
        Persistent
    }
}