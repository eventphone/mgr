using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace epmgr.Data.ywsd
{
    public class ForkRank
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("extension_id")]
        public int ExtensionId { get; set; }

        [ForeignKey(nameof(ExtensionId))]
        public Extension Extension { get; set; }

        [Column("index")]
        public int Index { get; set; }

        [Column("mode")]
        public ForkRankMode Mode { get; set; }

        [Column("delay")]
        public int? Delay { get; set; }

        public ICollection<ForkRankMember> ForkRankMember { get; set; }
    }

    public enum ForkRankMode
    {
        [PgName("DEFAULT")]
        Default,
        [PgName("NEXT")]
        Next,
        [PgName("DROP")]
        Drop
    }
}