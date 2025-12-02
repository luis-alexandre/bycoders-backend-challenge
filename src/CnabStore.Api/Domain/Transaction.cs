namespace CnabStore.Api.Domain;

public class Transaction
{
    public int Id { get; set; }

    public TransactionType Type { get; set; }

    /// <summary>
    /// Combined date and time when the transaction occurred.
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Transaction value already with the correct sign applied
    /// (positive for Income, negative for Expense).
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// CPF of the beneficiary as a numeric string (11 digits).
    /// </summary>
    public string Cpf { get; set; } = null!;

    /// <summary>
    /// Card used in the transaction.
    /// </summary>
    public string Card { get; set; } = null!;

    public int StoreId { get; set; }

    public Store Store { get; set; } = null!;
}
