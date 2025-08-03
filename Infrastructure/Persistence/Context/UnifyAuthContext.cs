using Domain.Entities;
using Infrastructure.Common.IdentityModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Context
{
    public class UnifyAuthContext : IdentityDbContext<IdentityUserModel, IdentityRole<Guid>, Guid>
    {
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public UnifyAuthContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RefreshToken>(entity => {
                entity.HasKey(rt => rt.Id);

                entity.HasIndex(rt => rt.UserId);

                entity.Property(rt => rt.Token).IsRequired();
                entity.HasIndex(rt => rt.Token).IsUnique();

                entity.Property(rt => rt.Expires).IsRequired();

                entity.Property(rt => rt.Created).IsRequired();

                entity.Property(rt => rt.Revoked).HasDefaultValue(false).IsRequired();
            });
        }
    }
}
