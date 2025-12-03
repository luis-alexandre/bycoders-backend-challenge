using CnabStore.Api.Application.Dtos;
using CnabStore.Api.Application.Interfaces;
using CnabStore.Api.Domain;
using System.Globalization;

namespace CnabStore.Api.Application;

/// <summary>
/// Responsible for parsing a single CNAB line into a TransactionDto.
/// Implements the fixed-width specification provided in the challenge.
/// </summary>
public class CnabLineParser : ICnabLineParser
{
    /// <inheritdoc />
    public TransactionDto ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            throw new ArgumentException("CNAB line cannot be null or whitespace.", nameof(line));
        }

        var typeStr = line.Substring(0, 1);
        var dateStr = line.Substring(1, 8);
        var valueStr = line.Substring(9, 10);
        var cpfStr = line.Substring(19, 11);
        var cardStr = line.Substring(30, 12);
        var timeStr = line.Substring(42, 6);
        var ownerStr = line.Substring(48, 14);
        var storeStr = line.Substring(62);

        // Type validation
        if (typeStr.Length != 1 || !char.IsDigit(typeStr[0]))
        {
            throw new ArgumentException($"Invalid transaction type format: '{typeStr}'.", nameof(line));
        }

        if (!int.TryParse(typeStr, out var typeInt) ||
            !Enum.IsDefined(typeof(TransactionType), typeInt))
        {
            throw new ArgumentException($"Invalid transaction type value: '{typeStr}'.", nameof(line));
        }

        // Date validation
        if (!dateStr.All(char.IsDigit))
        {
            throw new ArgumentException($"Invalid date format (non-digit characters): '{dateStr}'.", nameof(line));
        }

        if (!DateTime.TryParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
        {
            throw new ArgumentException($"Invalid date value: '{dateStr}'.", nameof(line));
        }

        // Time validation
        if (!timeStr.All(char.IsDigit))
        {
            throw new ArgumentException($"Invalid time format (non-digit characters): '{timeStr}'.", nameof(line));
        }

        if (!TimeOnly.TryParseExact(timeStr, "HHmmss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var time))
        {
            throw new ArgumentException($"Invalid time value: '{timeStr}'.", nameof(line));
        }

        var occurredAt = date.Add(time.ToTimeSpan());

        // Value validation (10 digits)
        if (valueStr.Length != 10 || !valueStr.All(char.IsDigit))
        {
            throw new ArgumentException($"Invalid transaction value format: '{valueStr}'.", nameof(line));
        }

        if (!decimal.TryParse(valueStr, NumberStyles.None, CultureInfo.InvariantCulture, out var rawValue))
        {
            throw new ArgumentException($"Invalid transaction value: '{valueStr}'.", nameof(line));
        }

        // CPF validation (11 digits)
        if (cpfStr.Length != 11 || !cpfStr.All(char.IsDigit))
        {
            throw new ArgumentException($"Invalid CPF format: '{cpfStr}'. Expected 11 digits.", nameof(line));
        }

        var cpf = cpfStr.Trim();

        // Card validation (12 chars, may contain masked chars)
        if (cardStr.Length != 12)
        {
            throw new ArgumentException($"Invalid card field length: '{cardStr}'. Expected 12 characters.", nameof(line));
        }

        var card = cardStr.Trim();
        if (string.IsNullOrWhiteSpace(card))
        {
            throw new ArgumentException("Card field cannot be empty.", nameof(line));
        }

        // Store owner validation (non-empty after trimming)
        var storeOwner = ownerStr.Trim();
        if (string.IsNullOrWhiteSpace(storeOwner))
        {
            throw new ArgumentException("Store owner cannot be empty.", nameof(line));
        }

        // Store name validation (non-empty after trimming)
        var storeName = storeStr.Trim();
        if (string.IsNullOrWhiteSpace(storeName))
        {
            throw new ArgumentException("Store name cannot be empty.", nameof(line));
        }

        var type = (TransactionType)typeInt;
        var metadata = TransactionTypeMetadata.Get(type);

        // Raw value must be divided by 100 and then sign applied (+1 or -1).
        var value = (rawValue / 100m) * metadata.Sign;

        return new TransactionDto(Type: typeInt,
                                  OccurredAt: occurredAt,
                                  Value: value,
                                  Cpf: cpf,
                                  Card: card,
                                  StoreOwner: storeOwner,
                                  StoreName: storeName);
    }
}
