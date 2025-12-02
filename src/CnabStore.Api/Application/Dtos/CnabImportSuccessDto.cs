namespace CnabStore.Api.Application.Dtos
{
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
