using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace epmgr.Data.ywsd
{
    public class Extension
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("yate_id")]
        public int? YateId { get; set; }

        [ForeignKey(nameof(YateId))]
        public Yate Yate { get; set; }

        [Column("extension")]
        [StringLength(32)]
        public string Number { get; set; }

        [Column("name")]
        [StringLength(64)]
        public string Name { get; set; }

        [Column("short_name")]
        [StringLength(8)]
        public string ShortName { get; set; }

        [Column("type")]
        public ExtensionType Type { get; set; }

        [Column("outgoing_extension")]
        [StringLength(32)]
        public string OutgoingNumber { get; set; }

        [Column("outgoing_name")]
        [StringLength(64)]
        public string OutgoingName { get; set; }

        [Column("dialout_allowed")]
        public bool IsDialoutAllowed { get; set; }

        [Column("ringback")]
        [StringLength(128)]
        public string Ringback { get; set; }

        [Column("forwarding_mode")]
        public ForwardingMode ForwardingMode { get; set; }

        [Column("forwarding_delay")]
        public int? ForwardingDelay { get; set; }

        [Column("forwarding_extension_id")]
        public int? ForwardingExtensionId { get; set; }

        [ForeignKey(nameof(ForwardingExtensionId))]
        public Extension ForwardingExtension { get; set; }

        [Column("lang")]
        [StringLength(6)]
        public string Language { get; set; }
        
        public ICollection<ForkRank> ForkRanks { get; set; }
    }

    public enum ForwardingMode
    {
        [PgName("DISABLED")]
        Disabled,
        [PgName("ENABLED")]
        Enabled,
        [PgName("ON_BUSY")]
        OnBusy,
        [PgName("ON_UNAVAILABLE")]
        OnUnavailable
    }

    public enum ExtensionType
    {
        [PgName("SIMPLE")]
        Simple,
        [PgName("MULTIRING")]
        Multiring,
        [PgName("GROUP")]
        Group,
        [PgName("EXTERNAL")]
        External,
        [PgName("TRUNK")]
        Trunk,
    }
}
