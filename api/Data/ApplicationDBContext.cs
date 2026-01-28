using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace api.Data
{
    
    //for API that doesnt have identity public class ApplicationDBContext: DbContext
    public class ApplicationDBContext: IdentityDbContext<AppUser>
    {
        public ApplicationDBContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
            
        }

        public DbSet<Stock> Stocks {get; set;}
        public DbSet<Comment> Comments {get; set;}
        public DbSet<Portfolio> Portfolios { get; set; }

        //Roles - for identity and JWT
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //for creating relationships(you can ignore it)
            builder.Entity<Portfolio>(x => x.HasKey(p => new {p.AppUserId, p.StockId}));
            builder.Entity<Portfolio>()
             .HasOne(u => u.Appuser)
             .WithMany(u => u.Portfolios)
             .HasForeignKey(p => p.AppUserId);
            builder.Entity<Portfolio>()
             .HasOne(u => u.Stock)
             .WithMany(u => u.Portfolios)
             .HasForeignKey(p => p.StockId);
            

            List<IdentityRole> roles = new List<IdentityRole>
            {
                new IdentityRole
                {
                    Id = "e29b36e4-dbc2-4244-b59e-267711744918",
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },

                new IdentityRole
                {
                    Id = "4bdb9f55-7041-4a69-bebc-b6865c17b8a0",
                    Name = "User",
                    NormalizedName = "USER"
                },
            };
            
            builder.Entity<IdentityRole>().HasData(roles);
        }
    }
}