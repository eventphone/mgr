using System;
using Microsoft.EntityFrameworkCore;

namespace epmgr.Data
{
    public class MgrDbContext : DbContext
    {
        public MgrDbContext()
        {
            //needed for Z.EntityFramework.Plus
        }

        public MgrDbContext(DbContextOptions<MgrDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.HasPostgresEnum<MgrExtensionType>();
            builder.Entity<MgrUser>().ToTable("User").HasKey(x=>x.Id);
            builder.Entity<MgrUser>().HasIndex(x => x.Username).IsUnique();
            builder.Entity<MgrUser>().HasData(new MgrUser
            {
                Id = 1, 
                LastLogon = DateTimeOffset.MinValue,
                Username = "mgr",
                PasswordHash = "AQAAAAEAACcQAAAAEB8YUrE2oHquTwLo4LGsvEmN10u7KUDg/AMreQ2sQLqfA2ln0hCaic1Grcc/qQ5+ew=="
            });
            builder.Entity<MgrExtension>().ToTable("Extension").HasKey(x => x.Id);
            builder.Entity<MgrExtension>().HasIndex(x => x.Extension).IsUnique();
            builder.Entity<MgrMessage>().ToTable("MessageQueue").HasKey(x => x.Id);
            builder.Entity<MgrMessage>().HasIndex(x => x.Timestamp);
            builder.Entity<MgrMessage>().HasIndex(x => x.Failed);
            builder.Entity<MgrDesasterCall>().ToTable("DesasterCall").HasKey(x => x.Id);
            builder.Entity<MgrDesasterCall>().HasIndex(x => x.Pin).IsUnique();
        }

        public DbSet<MgrUser> Users { get; set; }

        public DbSet<MgrExtension> Extensions { get; set; }

        public DbSet<MgrMessage> MessageQueue { get; set; }

        public DbSet<MgrDesasterCall> DesasterCalls { get; set; }
    }
}
