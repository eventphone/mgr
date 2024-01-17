using Microsoft.EntityFrameworkCore;

namespace epmgr.Data
{
    public class AppDbContext : DbContext, IYateDbContext<AppUser>
    {
        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
        {
            
        }

        public DbSet<AppUser> Users { get; set; }

        public DbSet<YateRegistration> Registrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AppUser>().ToTable("users");
            modelBuilder.Entity<YateRegistration>().ToTable("registrations").HasKey(x => new {x.Location, x.OConnectionId});
        }
    }
}