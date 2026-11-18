using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<WebApplication1.Models.Movie> Movies { get; set; }

        public DbSet<Booking> Bookings { get; set; }

        public DbSet<Screening> Screenings { get; set; }

        public DbSet<Promotion> Promotions { get; set; }
    }
}
