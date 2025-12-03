namespace CnabStore.Api.Application.Dtos
{
    /// <summary>
    /// Cnab import error details for a specific line in the import file.
    /// </summary>
    public class CnabImportErrorDto
    {
        public int LineNumber { get; set; }
        public string Error { get; set; }
        public string RawLine { get; set; }

        public CnabImportErrorDto(int lineNumber, string error, string rawLine)
        {
            LineNumber = lineNumber;
            Error = error;
            RawLine = rawLine;
        }
    }
}
