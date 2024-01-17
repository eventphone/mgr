using Microsoft.EntityFrameworkCore;

namespace epmgr.Data
{
    public class PremiumDbContext : DbContext, IYateDbContext<PremiumUser>
    {
        public PremiumDbContext()
        {
        }

        public PremiumDbContext(DbContextOptions<PremiumDbContext> options):base(options)
        {
        }

        public DbSet<PremiumUser> Users { get; set; }

        public DbSet<YateRegistration> Registrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<PremiumUser>().ToTable("users");
            modelBuilder.Entity<YateRegistration>().ToTable("registrations").HasKey(x => new {x.Location, x.OConnectionId});
        }
    }
}