namespace CnabStore.Api.Domain;

public class Store
{
    public int Id { get; set; }

    /// <summary>
    /// Store name (e.g., "LOJA DO Ó - MATRIZ").
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Store representative name (e.g., "MARIA JOSEFINA").
    /// </summary>
    public string OwnerName { get; set; } = null!;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    /// <summary>
    /// Convenience method to get the total balance in memory.
    /// This is not meant to be mapped as a database column.
    /// </summary>
    public decimal GetTotalBalance() => Transactions.Sum(t => t.Value);
}
