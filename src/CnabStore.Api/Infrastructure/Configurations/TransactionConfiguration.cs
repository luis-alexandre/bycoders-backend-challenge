using CnabStore.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CnabStore.Api.Infrastructure.Configurations;

/// <summary>
/// Entity Framework mapping configuration for the Transaction entity.
/// </summary>
public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type)
               .HasConversion<int>()  // store enum as int
               .IsRequired();

        builder.Property(t => t.OccurredAt)
               .IsRequired();

        builder.Property(t => t.Value)
               .HasColumnType("numeric(18,2)")
               .IsRequired();

        builder.Property(t => t.Cpf)
               .IsRequired()
               .HasMaxLength(11);

        builder.Property(t => t.Card)
               .IsRequired()
               .HasMaxLength(12);

        builder.HasOne(t => t.Store)
               .WithMany(s => s.Transactions)
               .HasForeignKey(t => t.StoreId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.StoreId);
        builder.HasIndex(t => t.OccurredAt);
        builder.HasIndex(t => t.Cpf);
    }
}
