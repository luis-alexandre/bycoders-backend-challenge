using CnabStore.Api.Application;
using CnabStore.Api.Application.Dtos;
using CnabStore.Api.Application.Interfaces;
using FluentAssertions;

namespace CnabStore.Tests;

/// <summary>
/// Unit tests for CnabLineParser.
/// </summary>
public class CnabLineParserTests
{
    private readonly ICnabLineParser _parser = new CnabLineParser();

    [Fact]
    public void ParseLine_ShouldParseValidLineCorrectly()
    {
        // Arrange
        const string line =
            "1201903010000010000123456789011234****5678153000BAR DO JOAO   LOJA DO O - MATRIZ ";

        // Act
        TransactionDto dto = _parser.ParseLine(line);

        // Assert
        dto.Type.Should().Be(1);
        dto.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss").Should().Be("2019-03-01 15:30:00");
        dto.Value.Should().Be(100.00m); // Debit => Income => positive
        dto.Cpf.Should().Be("12345678901");
        dto.Card.Should().Be("1234****5678");
        dto.StoreOwner.Should().Be("BAR DO JOAO");
        dto.StoreName.Should().Be("LOJA DO O - MATRIZ");
    }

    [Fact]
    public void ParseLine_ShouldThrow_WhenLineIsTooShort()
    {
        // Arrange
        const string line = "123"; // invalid length

        // Act
        var act = () => _parser.ParseLine(line);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*Invalid CNAB line length*");
    }

    [Fact]
    public void ParseLine_ShouldThrow_WhenTypeIsInvalid()
    {
        // Arrange
        // Same as valid line but first char (type) is 0 (invalid)
        const string line =
            "0201903010000010000123456789011234****5678153000BAR DO JOAO   LOJA DO O - MATRIZ ";

        // Act
        var act = () => _parser.ParseLine(line);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*Invalid transaction type*");
    }

    [Fact]
    public void ParseLine_ShouldThrow_WhenValueIsInvalid()
    {
        // Arrange
        // Replace numeric value segment with invalid chars: "ABCDEFGHIJ"
        const string line =
            "120190301ABCDEFGHIJ123456789011234****5678153000BAR DO JOAO   LOJA DO O - MATRIZ ";

        // Act
        var act = () => _parser.ParseLine(line);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*Invalid transaction value*");
    }
}
