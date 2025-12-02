using CnabStore.Api.Application;
using CnabStore.Api.Application.Dtos;
using CnabStore.Api.Application.Interfaces;
using CnabStore.Api.Domain;
using CnabStore.Api.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text;
using Xunit;

namespace CnabStore.Tests;

/// <summary>
/// Unit tests for CnabImportService using an InMemory AppDbContext.
/// </summary>
public class CnabImportServiceTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"CnabStoreTests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task ImportAsync_ShouldIgnoreEmptyLines()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();

        var parserMock = new Mock<ICnabLineParser>();

        var line1 = "line-1";

        var dto1 = new TransactionDto(
            Type: (int)TransactionType.Sales,
            OccurredAt: new DateTime(2024, 01, 02, 9, 0, 0),
            Value: 200.00m,
            Cpf: "98765432100",
            Card: "3333****3333",
            StoreOwner: "JANE DOE",
            StoreName: "STORE B"
        );

        parserMock.Setup(p => p.ParseLine(line1)).Returns(dto1);

        var service = new CnabImportService(dbContext, parserMock.Object);

        var cnabContent = $"{line1}\n\n   \n"; // multiple empty/whitespace lines
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(cnabContent));

        // Act
        await service.ImportAsync(stream);

        // Assert
        var stores = dbContext.Stores.Include(s => s.Transactions).ToList();

        stores.Should().HaveCount(1);
        var store = stores.Single();
        store.Transactions.Should().HaveCount(1);

        parserMock.Verify(p => p.ParseLine(line1), Times.Once);
        parserMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportAsync_ShouldCreateNewStore_WhenStoreDoesNotExist()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();

        var parserMock = new Mock<ICnabLineParser>();

        var line = "line-1";

        var dto = new TransactionDto(
            Type: (int)TransactionType.Credit,
            OccurredAt: new DateTime(2024, 01, 03, 14, 30, 0),
            Value: 300.00m,
            Cpf: "11122233344",
            Card: "4444****4444",
            StoreOwner: "ALICE",
            StoreName: "STORE C"
        );

        parserMock.Setup(p => p.ParseLine(line)).Returns(dto);

        var service = new CnabImportService(dbContext, parserMock.Object);

        var cnabContent = line;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(cnabContent));

        // Act
        await service.ImportAsync(stream);

        // Assert
        var store = dbContext.Stores.Include(s => s.Transactions).Single();

        store.Name.Should().Be("STORE C");
        store.OwnerName.Should().Be("ALICE");
        store.Transactions.Should().HaveCount(1);
        store.Transactions.Single().Value.Should().Be(300.00m);
    }

    [Fact]
    public async Task ImportAsync_ShouldReuseExistingStore_WhenSameNameAndOwner()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();

        // Seed an existing store
        var existingStore = new Store
        {
            Name = "STORE D",
            OwnerName = "BOB"
        };
        dbContext.Stores.Add(existingStore);
        await dbContext.SaveChangesAsync();

        var parserMock = new Mock<ICnabLineParser>();

        var line = "line-1";

        var dto = new TransactionDto(
            Type: (int)TransactionType.Sales,
            OccurredAt: new DateTime(2024, 01, 04, 16, 0, 0),
            Value: 500.00m,
            Cpf: "55566677788",
            Card: "5555****5555",
            StoreOwner: "BOB",
            StoreName: "STORE D"
        );

        parserMock.Setup(p => p.ParseLine(line)).Returns(dto);

        var service = new CnabImportService(dbContext, parserMock.Object);

        var cnabContent = line;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(cnabContent));

        // Act
        await service.ImportAsync(stream);

        // Assert
        var stores = dbContext.Stores.Include(s => s.Transactions).ToList();

        stores.Should().HaveCount(1, "the existing store should be reused instead of creating a new one");

        var store = stores.Single();
        store.Name.Should().Be("STORE D");
        store.OwnerName.Should().Be("BOB");
        store.Transactions.Should().HaveCount(1);
        store.Transactions.Single().Value.Should().Be(500.00m);
    }
}
