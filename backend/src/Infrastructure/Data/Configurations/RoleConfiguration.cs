using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RoleName).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.RoleName).IsUnique();

        builder.HasData(
            new Role {Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), RoleName = RoleName.ADMIN},
            new Role {Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), RoleName = RoleName.EVENT_PLANNER},
            new Role {Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), RoleName = RoleName.VENDOR}
        );
    }
}