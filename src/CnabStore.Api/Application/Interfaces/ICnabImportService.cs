namespace CnabStore.Api.Application.Interfaces;

/// <summary>
/// Abstraction for the CNAB import application service.
/// </summary>
public interface ICnabImportService
{
    /// <summary>
    /// Imports a CNAB file from a stream and persists all transactions.
    /// </summary>
    Task ImportAsync(Stream cnabStream, CancellationToken cancellationToken = default);
}
