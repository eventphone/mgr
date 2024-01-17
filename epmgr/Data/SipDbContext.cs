using Microsoft.EntityFrameworkCore;

namespace epmgr.Data
{
    public class SipDbContext : DbContext, IYateDbContext<SipUser>
    {
        public SipDbContext()
        {
        }

        public SipDbContext(DbContextOptions<SipDbContext> options):base(options)
        {
        }

        public DbSet<SipUser> Users { get; set; }

        public DbSet<YateRegistration> Registrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SipUser>().ToTable("users");
            modelBuilder.Entity<YateRegistration>().ToTable("registrations").HasKey(x => new {x.Location, x.OConnectionId});
        }
    }
}