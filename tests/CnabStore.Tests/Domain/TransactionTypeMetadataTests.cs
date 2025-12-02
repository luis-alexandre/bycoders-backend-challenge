using CnabStore.Api.Domain;
using Xunit;

namespace CnabStore.Tests;

/// <summary>
/// Unit tests for the TransactionTypeMetadata helper.
/// </summary>
public class TransactionTypeMetadataTests
{
    [Fact]
    public void All_ShouldContainMetadata_ForEveryTransactionType()
    {
        // Arrange
        var allTypes = Enum.GetValues<TransactionType>();

        // Act
        var metadataDict = TransactionTypeMetadata.All;

        // Assert
        foreach (var type in allTypes)
        {
            Assert.True(
                metadataDict.ContainsKey(type),
                $"Expected metadata for transaction type '{type}' but it was not found in TransactionTypeMetadata.All."
            );
        }
    }

    [Fact]
    public void Get_ShouldReturnMetadata_ForKnownType()
    {
        // Arrange
        var type = TransactionType.Debit;

        // Act
        var metadata = TransactionTypeMetadata.Get(type);

        // Assert
        Assert.Equal(type, metadata.Type);
        Assert.Equal("Debit", metadata.Description);
        Assert.Equal("Income", metadata.Nature);
        Assert.Equal(1, metadata.Sign);
    }

    [Fact]
    public void Get_ShouldThrow_ForUnknownType()
    {
        // Arrange
        var invalidType = (TransactionType)999;

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => TransactionTypeMetadata.Get(invalidType)
        );

        Assert.Contains("Unknown transaction type", exception.Message);
    }

    [Theory]
    [InlineData(TransactionType.Debit, "Income", 1)]
    [InlineData(TransactionType.Credit, "Income", 1)]
    [InlineData(TransactionType.LoanReceipt, "Income", 1)]
    [InlineData(TransactionType.Sales, "Income", 1)]
    [InlineData(TransactionType.TedReceipt, "Income", 1)]
    [InlineData(TransactionType.DocReceipt, "Income", 1)]
    [InlineData(TransactionType.Boleto, "Expense", -1)]
    [InlineData(TransactionType.Financing, "Expense", -1)]
    [InlineData(TransactionType.Rent, "Expense", -1)]
    public void Metadata_ShouldHaveExpectedNatureAndSign(
        TransactionType type,
        string expectedNature,
        int expectedSign)
    {
        // Act
        var metadata = TransactionTypeMetadata.Get(type);

        // Assert
        Assert.Equal(expectedNature, metadata.Nature);
        Assert.Equal(expectedSign, metadata.Sign);
    }

    [Fact]
    public void All_ShouldBeReadOnly()
    {
        // Arrange
        var metadataDict = TransactionTypeMetadata.All;

        // Act & Assert
        Assert.ThrowsAny<NotSupportedException>(() =>
        {
            // Attempt to cast and mutate the dictionary to ensure it is read-only.
            var mutable = (IDictionary<TransactionType, TransactionTypeMetadata>)metadataDict;
            mutable.Remove(TransactionType.Debit);
        });
    }
}
