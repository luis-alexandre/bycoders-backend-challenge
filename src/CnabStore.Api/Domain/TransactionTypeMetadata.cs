using System.Collections.ObjectModel;

namespace CnabStore.Api.Domain;

/// <summary>
/// Static metadata for each transaction type:
/// - Description: human-friendly description
/// - Nature: "Income" or "Expense"
/// - Sign: +1 or -1 used to calculate the final balance
/// </summary>
public sealed record TransactionTypeMetadata(TransactionType Type,
                                             string Description,
                                             string Nature,
                                             int Sign)
{
    private static readonly IReadOnlyDictionary<TransactionType, TransactionTypeMetadata> _all =
        new ReadOnlyDictionary<TransactionType, TransactionTypeMetadata>(
            new Dictionary<TransactionType, TransactionTypeMetadata>
            {
                [TransactionType.Debit] = new(TransactionType.Debit, "Debit", "Income", +1),
                [TransactionType.Boleto] = new(TransactionType.Boleto, "Boleto", "Expense", -1),
                [TransactionType.Financing] = new(TransactionType.Financing, "Financing", "Expense", -1),
                [TransactionType.Credit] = new(TransactionType.Credit, "Credit", "Income", +1),
                [TransactionType.LoanReceipt] = new(TransactionType.LoanReceipt, "Loan Receipt", "Income", +1),
                [TransactionType.Sales] = new(TransactionType.Sales, "Sales", "Income", +1),
                [TransactionType.TedReceipt] = new(TransactionType.TedReceipt, "TED Receipt", "Income", +1),
                [TransactionType.DocReceipt] = new(TransactionType.DocReceipt, "DOC Receipt", "Income", +1),
                [TransactionType.Rent] = new(TransactionType.Rent, "Rent", "Expense", -1),
            });

    public static IReadOnlyDictionary<TransactionType, TransactionTypeMetadata> All => _all;

    public static TransactionTypeMetadata Get(TransactionType type)
    {
        if (!_all.TryGetValue(type, out var metadata))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown transaction type.");
        }

        return metadata;
    }
}
