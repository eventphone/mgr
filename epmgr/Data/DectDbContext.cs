using Microsoft.EntityFrameworkCore;

namespace epmgr.Data
{
    public class DectDbContext : DbContext, IYateDbContext<DectUser>
    {
        public DectDbContext()
        {
        }

        public DectDbContext(DbContextOptions<DectDbContext> options):base(options)
        {
        }

        public DbSet<DectUser> Users { get; set; }

        public DbSet<YateRegistration> Registrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasPostgresEnum<DectDisplayModus>();
            modelBuilder.Entity<DectUser>().ToTable("users");
            modelBuilder.Entity<YateRegistration>().ToTable("registrations").HasKey(x => new {x.Location, x.OConnectionId});
        }
    }

    public interface IYateDbContext<TUser> where TUser:YateUser
    {
        DbSet<TUser> Users { get; }

        DbSet<YateRegistration> Registrations { get; }
    }
}