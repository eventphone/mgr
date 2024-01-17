using Microsoft.EntityFrameworkCore;

namespace epmgr.Data.ywsd
{
    public class YwsdDbContext : DbContext
    {
        public YwsdDbContext()
        {
        }

        public YwsdDbContext(DbContextOptions<YwsdDbContext> options):base(options)
        {
        }

        public DbSet<Yate> Yates { get; set; }

        public DbSet<Extension> Extensions { get; set; }

        public DbSet<ForkRank> ForkRanks { get; set; }

        public DbSet<ForkRankMember> ForkRankMember { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.HasPostgresEnum<ExtensionType>();
            builder.HasPostgresEnum<ForwardingMode>();
            builder.HasPostgresEnum<ForkRankMode>();
            builder.HasPostgresEnum<ForkRankMemberType>();
            builder.Entity<Yate>().ToTable("Yate");
            builder.Entity<Extension>().ToTable("Extension");
            builder.Entity<ForkRank>().ToTable("ForkRank");
            builder.Entity<ForkRankMember>().ToTable("ForkRankMember").HasKey(x => new {x.ForkRankId, x.ExtensionId});
        }
    }
}