using System.Globalization;
using CnabStore.Api.Application.Dtos;
using CnabStore.Api.Application.Interfaces;
using CnabStore.Api.Domain;

namespace CnabStore.Api.Application;

/// <summary>
/// Responsible for parsing a single CNAB line into a TransactionDto.
/// </summary>
public class CnabLineParser : ICnabLineParser
{
    /// <summary>
    /// Parses a single CNAB line according to the specification.
    /// </summary>
    public TransactionDto ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || line.Length < 81)
        {
            throw new ArgumentException("Invalid CNAB line length.", nameof(line));
        }

        var typeStr = line[..1];
        var dateStr = line.Substring(1, 8);
        var valueStr = line.Substring(9, 10);
        var cpf = line.Substring(19, 11).Trim();
        var card = line.Substring(30, 12).Trim();
        var timeStr = line.Substring(42, 6);
        var storeOwner = line.Substring(48, 14).Trim();
        var storeName = line.Substring(62, 19).Trim();

        if (!int.TryParse(typeStr, out var typeInt) ||
            !Enum.IsDefined(typeof(TransactionType), typeInt))
        {
            throw new ArgumentException($"Invalid transaction type: {typeStr}", nameof(line));
        }

        var date = DateTime.ParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture);
        var time = TimeOnly.ParseExact(timeStr, "HHmmss", CultureInfo.InvariantCulture);
        var occurredAt = date.Add(time.ToTimeSpan()); // ignoring timezone offset on purpose

        if (!decimal.TryParse(valueStr,
                              NumberStyles.None,
                              CultureInfo.InvariantCulture,
                              out var rawValue))
        {
            throw new ArgumentException($"Invalid transaction value: {valueStr}", nameof(line));
        }

        var type = (TransactionType)typeInt;
        var metadata = TransactionTypeMetadata.Get(type);

        var value = (rawValue / 100m) * metadata.Sign;

        return new TransactionDto(Type: typeInt,
                                  OccurredAt: occurredAt,
                                  Value: value,
                                  Cpf: cpf,
                                  Card: card,
                                  StoreOwner: storeOwner,
                                  StoreName: storeName
        );
    }
}
