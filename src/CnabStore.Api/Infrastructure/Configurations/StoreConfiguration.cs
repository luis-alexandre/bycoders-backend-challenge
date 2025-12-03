using CnabStore.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CnabStore.Api.Infrastructure.Configurations;

/// <summary>
/// Entity Framework mapping configuration for the Store entity.
/// </summary>
public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("stores");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(s => s.OwnerName)
               .IsRequired()
               .HasMaxLength(100);

        builder.HasMany(s => s.Transactions)
               .WithOne(t => t.Store)
               .HasForeignKey(t => t.StoreId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.Name);
        builder.HasIndex(s => s.OwnerName);

        builder.HasIndex(s => new { s.Name, s.OwnerName })
               .IsUnique()
               .HasDatabaseName("UX_Stores_Name_OwnerName");
    }
}
