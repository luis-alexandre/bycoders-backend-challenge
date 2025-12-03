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
    public async Task ImportAsync_ShouldPersistStoreAndTransactions_ForMultipleValidLines()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();

        var parserMock = new Mock<ICnabLineParser>();

        var line1 = "line-1";
        var line2 = "line-2";

        // Both lines belong to the same store to validate upsert behavior.
        var dto1 = new TransactionDto(Type: (int)TransactionType.Debit,
                                      OccurredAt: new DateTime(2024, 01, 01, 10, 0, 0),
                                      Value: 100.00m,
                                      Cpf: "12345678901",
                                      Card: "1111****1111",
                                      StoreOwner: "JOHN DOE",
                                      StoreName: "STORE A");

        var dto2 = new TransactionDto(Type: (int)TransactionType.Boleto,
                                      OccurredAt: new DateTime(2024, 01, 01, 11, 0, 0),
                                      Value: -50.00m,
                                      Cpf: "12345678901",
                                      Card: "2222****2222",
                                      StoreOwner: "JOHN DOE",
                                      StoreName: "STORE A");

        parserMock.Setup(p => p.ParseLine(line1)).Returns(dto1);
        parserMock.Setup(p => p.ParseLine(line2)).Returns(dto2);

        var service = new CnabImportService(dbContext, parserMock.Object);

        // Note: an empty last line is ignored (not counted in TotalLines).
        var cnabContent = $"{line1}\n{line2}\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(cnabContent));

        // Act
        var result = await service.ImportAsync(stream);

        // Assert (result)
        result.TotalLines.Should().Be(2);
        result.ImportedCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        result.Imported.Should().HaveCount(2);
        result.Failed.Should().BeEmpty();

        result.Imported.Select(x => x.LineNumber).Should().BeEquivalentTo(new[] { 1, 2 });

        // Assert (database)
        var stores = dbContext.Stores.Include(s => s.Transactions).ToList();

        stores.Should().HaveCount(1);
        var store = stores.Single();
        store.Name.Should().Be("STORE A");
        store.OwnerName.Should().Be("JOHN DOE");
        store.Transactions.Should().HaveCount(2);

        var totalBalance = store.Transactions.Sum(t => t.Value);
        totalBalance.Should().Be(50.00m); // 100 - 50

        parserMock.Verify(p => p.ParseLine(line1), Times.Once);
        parserMock.Verify(p => p.ParseLine(line2), Times.Once);
        parserMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportAsync_ShouldReportFailures_WhenParserThrows()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();

        var parserMock = new Mock<ICnabLineParser>();

        var validLine = "line-valid";
        var invalidLine = "line-invalid";

        var validDto = new TransactionDto(Type: (int)TransactionType.Sales,
                                          OccurredAt: new DateTime(2024, 01, 02, 9, 0, 0),
                                          Value: 200.00m,
                                          Cpf: "98765432100",
                                          Card: "3333****3333",
                                          StoreOwner: "JANE DOE",
                                          StoreName: "STORE B");

        parserMock.Setup(p => p.ParseLine(validLine)).Returns(validDto);
        parserMock.Setup(p => p.ParseLine(invalidLine))
                  .Throws(new ArgumentException("Invalid transaction value format: 'ABCDEFGHIJ'."));

        var service = new CnabImportService(dbContext, parserMock.Object);

        var cnabContent = $"{validLine}\n{invalidLine}\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(cnabContent));

        // Act
        var result = await service.ImportAsync(stream);

        // Assert (result)
        result.TotalLines.Should().Be(2);
        result.ImportedCount.Should().Be(1);
        result.FailedCount.Should().Be(1);

        result.Imported.Should().HaveCount(1);
        result.Failed.Should().HaveCount(1);

        var imported = result.Imported.Single();
        imported.LineNumber.Should().Be(1);
        imported.Transaction.StoreName.Should().Be("STORE B");

        var failed = result.Failed.Single();
        failed.LineNumber.Should().Be(2);
        failed.Error.Should().Contain("Invalid transaction value format");
        failed.RawLine.Should().Be(invalidLine);

        // Assert (database)
        var stores = dbContext.Stores.Include(s => s.Transactions).ToList();

        stores.Should().HaveCount(1);
        var store = stores.Single();
        store.Name.Should().Be("STORE B");
        store.OwnerName.Should().Be("JANE DOE");
        store.Transactions.Should().HaveCount(1);
        store.Transactions.Single().Value.Should().Be(200.00m);
    }

    [Fact]
    public async Task ImportAsync_ShouldIgnoreEmptyAndWhitespaceLines()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();

        var parserMock = new Mock<ICnabLineParser>();

        var line1 = "line-1";
        var line2 = "line-2";

        var dto1 = new TransactionDto(Type: (int)TransactionType.Sales,
                                      OccurredAt: new DateTime(2024, 01, 03, 10, 0, 0),
                                      Value: 150.00m,
                                      Cpf: "11111111111",
                                      Card: "4444****4444",
                                      StoreOwner: "OWNER 1",
                                      StoreName: "STORE X");

        var dto2 = new TransactionDto(Type: (int)TransactionType.Credit,
                                      OccurredAt: new DateTime(2024, 01, 03, 11, 0, 0),
                                      Value: 250.00m,
                                      Cpf: "22222222222",
                                      Card: "5555****5555",
                                      StoreOwner: "OWNER 1",
                                      StoreName: "STORE X");

        parserMock.Setup(p => p.ParseLine(line1)).Returns(dto1);
        parserMock.Setup(p => p.ParseLine(line2)).Returns(dto2);

        var service = new CnabImportService(dbContext, parserMock.Object);

        // Empty/whitespace lines should be ignored completely (not counted in TotalLines)
        var cnabContent = $"{line1}\n\n   \n{line2}\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(cnabContent));

        // Act
        var result = await service.ImportAsync(stream);

        // Assert
        result.TotalLines.Should().Be(2);
        result.ImportedCount.Should().Be(2);
        result.FailedCount.Should().Be(0);

        parserMock.Verify(p => p.ParseLine(line1), Times.Once);
        parserMock.Verify(p => p.ParseLine(line2), Times.Once);
        parserMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ImportAsync_ShouldReuseExistingStore_WhenSameNameAndOwnerAlreadyInDatabase()
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

        var dto = new TransactionDto(Type: (int)TransactionType.Sales,
                                     OccurredAt: new DateTime(2024, 01, 04, 16, 0, 0),
                                     Value: 500.00m,
                                     Cpf: "55566677788",
                                     Card: "5555****5555",
                                     StoreOwner: "BOB",        // same as existing
                                     StoreName: "STORE D");      // same as existing

        parserMock.Setup(p => p.ParseLine(line)).Returns(dto);

        var service = new CnabImportService(dbContext, parserMock.Object);

        var cnabContent = line;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(cnabContent));

        // Act
        var result = await service.ImportAsync(stream);

        // Assert
        result.TotalLines.Should().Be(1);
        result.ImportedCount.Should().Be(1);
        result.FailedCount.Should().Be(0);

        var stores = dbContext.Stores.Include(s => s.Transactions).ToList();

        stores.Should().HaveCount(1, "the existing store should be reused instead of creating a new one");

        var store = stores.Single();
        store.Name.Should().Be("STORE D");
        store.OwnerName.Should().Be("BOB");
        store.Transactions.Should().HaveCount(1);
        store.Transactions.Single().Value.Should().Be(500.00m);
    }
}
