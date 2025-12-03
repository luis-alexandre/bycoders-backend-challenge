namespace CnabStore.Api.Application.Dtos
{
    /// <summary>
    /// Cnab import details for a specific line in the import file.
    /// </summary>
    public class CnabImportSuccessDto
    {
        public int LineNumber { get; set; }
        public TransactionDto Transaction { get; set; }

        public CnabImportSuccessDto(int lineNumber, TransactionDto transaction)
        {
            LineNumber = lineNumber;
            Transaction = transaction;
        }
    }
}
