using MusicSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MusicSystem.Areas.Identity.Data;

namespace MusicSystem.Data
{
    public class MusicSystemContext : IdentityDbContext<MusicSystemUser>
    {
        public MusicSystemContext(DbContextOptions<MusicSystemContext> options)
            : base(options)
        {
        }

        public DbSet<Artist> Artist { get; set; }
        public DbSet<Song> Song { get; set; }
        public DbSet<MusicSystemUser> MusicSystemUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
}
