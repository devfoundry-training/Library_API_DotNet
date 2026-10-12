using Library.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Api.Data.Configurations;

/// <summary>The members table, matching db/schema.sql in the contract repo.</summary>
public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> member)
    {
        member.ToTable("members", table =>
            table.HasCheckConstraint("ck_members_type", "type IN ('standard', 'student', 'staff')"));

        member.HasKey(m => m.Id);
        member.Property(m => m.Id).HasColumnName("id");
        member.Property(m => m.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        member.Property(m => m.Email).HasColumnName("email").HasMaxLength(100).IsRequired();
        member.Property(m => m.Type).HasColumnName("type").HasMaxLength(10).IsRequired();
        member.Property(m => m.Joined).HasColumnName("joined");

        member.HasIndex(m => m.Email).IsUnique().HasDatabaseName("ux_members_email");
    }
}
