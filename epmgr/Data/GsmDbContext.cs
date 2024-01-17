using Microsoft.EntityFrameworkCore;

namespace epmgr.Data
{
    public class GsmDbContext : DbContext, IYateDbContext<GsmUser>
    {
        public GsmDbContext()
        {
        }

        public GsmDbContext(DbContextOptions<GsmDbContext> options):base(options)
        {
        }

        public DbSet<GsmUser> Users { get; set; }

        public DbSet<YateRegistration> Registrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<GsmUser>().ToTable("users");
            modelBuilder.Entity<YateRegistration>().ToTable("registrations").HasKey(x => new {x.Location, x.OConnectionId});
        }
    }
}