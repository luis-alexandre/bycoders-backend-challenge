using CnabStore.Api.Application;
using CnabStore.Api.Application.Dtos;
using CnabStore.Api.Application.Interfaces;
using CnabStore.Api.Domain;
using FluentAssertions;

namespace CnabStore.Tests;

/// <summary>
/// Unit tests for CnabLineParser.
/// </summary>
public class CnabLineParserTests
{
    private readonly ICnabLineParser _parser = new CnabLineParser();

    /// <summary>
    /// Helper to build a valid CNAB line based on field values,
    /// respecting all sizes defined in the specification.
    /// </summary>
    private static string BuildLine(string type,
                                    string date,
                                    string value,
                                    string cpf,
                                    string card,
                                    string time,
                                    string storeOwner,
                                    string storeName)
    {
        // Respect field lengths:
        // type: 1
        // date: 8
        // value: 10
        // cpf: 11
        // card: 12
        // time: 6
        // owner: 14
        // store: 19

        type = type.PadLeft(1).Substring(0, 1);
        date = date.PadLeft(8, '0').Substring(0, 8);
        value = value.PadLeft(10, '0').Substring(0, 10);
        cpf = cpf.PadLeft(11, '0').Substring(0, 11);
        card = card.PadRight(12, ' ').Substring(0, 12);
        time = time.PadLeft(6, '0').Substring(0, 6);
        storeOwner = storeOwner.PadRight(14, ' ').Substring(0, 14);
        storeName = storeName.PadRight(19, ' ').Substring(0, 19);

        return string.Concat(type, date, value, cpf, card, time, storeOwner, storeName);
    }

    [Fact]
    public void ParseLine_ShouldParseValidLineCorrectly()
    {
        // Arrange
        var type = "1";                 // Debit (Income, +)
        var date = "20190301";
        var value = "0000010000";       // 100.00
        var cpf = "12345678901";
        var card = "1234****5678";
        var time = "153000";            // 15:30:00
        var owner = "BAR DO JOAO";
        var store = "LOJA DO O - MATRIZ";

        var line = BuildLine(type, date, value, cpf, card, time, owner, store);

        // Act
        TransactionDto dto = _parser.ParseLine(line);

        // Assert
        dto.Type.Should().Be((int)TransactionType.Debit);
        dto.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss").Should().Be("2019-03-01 15:30:00");
        dto.Value.Should().Be(100.00m); // Debit => Income => positive
        dto.Cpf.Should().Be("12345678901");
        dto.Card.Should().Be("1234****5678");
        dto.StoreOwner.Should().Be("BAR DO JOAO");
        dto.StoreName.Should().Be("LOJA DO O - MATRIZ");
    }

    [Fact]
    public void ParseLine_ShouldThrow_WhenLineIsNullOrWhitespace()
    {
        // Arrange
        string line = "   ";

        // Act
        var act = () => _parser.ParseLine(line);

        // Assert
        act.Should()
           .Throw<ArgumentException>()
           .WithMessage("*cannot be null or whitespace*");
    }

    [Fact]
    public void ParseLine_ShouldThrow_WhenTypeHasInvalidFormat()
    {
        // Arrange
        var line = BuildLine(type: "X", // invalid
                             date: "20190301",
                             value: "0000010000",
                             cpf: "12345678901",
                             card: "1234****5678",
                             time: "153000",
                             storeOwner: "OWNER",
                             storeName: "STORE");

        // Act
        var act = () => _parser.ParseLine(line);

        // Assert
        act.Should()
           .Throw<ArgumentException>()
           .WithMessage("*Invalid transaction type format*");
    }

    [Fact]
    public void ParseLine_ShouldThrow_WhenTypeIsOutOfRange()
    {
        // Arrange
        var line = BuildLine(type: "0",  // not mapped to enum
                             date: "20190301",
                             value: "0000010000",
                             cpf: "12345678901",
                             card: "1234****5678",
                             time: "153000",
                             storeOwner: "OWNER",
                             storeName: "STORE");

        // Act
        var act = () => _parser.ParseLine(line);

        // Assert
        act.Should()
           .Throw<ArgumentException>()
           .WithMessage("*Invalid transaction type value*");
    }

    [Fact]
    public void ParseLine_ShouldThrow_WhenDateHasNonDigits()
    {
        // Arrange
        var line = BuildLine(type: "1",
                             date: "2019AA01", // invalid
                             value: "0000010000",
                             cpf: "12345678901",
                             card: "1234****5678",
                             time: "153000",
                             storeOwner: "OWNER",
                             storeName: "STORE");

        // Act
        var act = () => _parser.ParseLine(line);

        // Assert
        act.Should()
           .Throw<ArgumentException>()
           .WithMessage("*Invalid date format*");
    }

    [Fact]
    public void ParseLine_ShouldThrow_WhenTimeHasNonDigits()
    {
        // Arrange
        var line = BuildLine(type: "1",
                             date: "20190301",
                             value: "0000010000",
                             cpf: "12345678901",
                             card: "1234****5678",
                             time: "15AA00", // invalid
                             storeOwner: "OWNER",
                             storeName: "STORE");

        // Act
        var act = () => _parser.ParseLine(line);

        // Assert
        act.Should()
           .Throw<ArgumentException>()
           .WithMessage("*Invalid time format*");
    }

    [Fact]
    public void ParseLine_ShouldThrow_WhenValueHasNonDigits()
    {
        // Arrange
        var line = BuildLine(type: "1",
                             date: "20190301",
                             value: "ABCDEFGHIJ", // invalid
                             cpf: "12345678901",
                             card: "1234****5678",
                             time: "153000",
                             storeOwner: "OWNER",
                             storeName: "STORE");

        // Act
        var act = () => _parser.ParseLine(line);

        // Assert
        act.Should()
           .Throw<ArgumentException>()
           .WithMessage("*Invalid transaction value format*");
    }


    [Fact]
    public void ParseLine_ShouldThrow_WhenCardIsEmptyAfterTrim()
    {
        // Arrange
        var line = BuildLine(type: "1",
                             date: "20190301",
                             value: "0000010000",
                             cpf: "12345678901",
                             card: "            ", // 12 spaces
                             time: "153000",
                             storeOwner: "OWNER",
                             storeName: "STORE");

        // Act
        var act = () => _parser.ParseLine(line);

        // Assert
        act.Should()
           .Throw<ArgumentException>()
           .WithMessage("*Card field cannot be empty*");
    }

    [Fact]
    public void ParseLine_ShouldThrow_WhenStoreOwnerIsEmpty()
    {
        // Arrange
        var line = BuildLine(type: "1",
                             date: "20190301",
                             value: "0000010000",
                             cpf: "12345678901",
                             card: "1234****5678",
                             time: "153000",
                             storeOwner: "   ", // empty after trim
                             storeName: "STORE");

        // Act
        var act = () => _parser.ParseLine(line);

        // Assert
        act.Should()
           .Throw<ArgumentException>()
           .WithMessage("*Store owner cannot be empty*");
    }

    [Fact]
    public void ParseLine_ShouldThrow_WhenStoreNameIsEmpty()
    {
        // Arrange
        var line = BuildLine(type: "1",
                             date: "20190301",
                             value: "0000010000",
                             cpf: "12345678901",
                             card: "1234****5678",
                             time: "153000",
                             storeOwner: "OWNER",
                             storeName: "     "); // empty after trim

        // Act
        var act = () => _parser.ParseLine(line);

        // Assert
        act.Should()
           .Throw<ArgumentException>()
           .WithMessage("*Store name cannot be empty*");
    }
}
