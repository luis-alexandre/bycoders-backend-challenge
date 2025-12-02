namespace CnabStore.Api.Application.Dtos
{
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
