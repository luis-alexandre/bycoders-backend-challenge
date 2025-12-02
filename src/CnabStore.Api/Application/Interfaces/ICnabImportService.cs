using CnabStore.Api.Application.Dtos;

namespace CnabStore.Api.Application;

/// <summary>
/// Abstraction for the CNAB import application service.
/// </summary>
public interface ICnabImportService
{
    /// <summary>
    /// Imports a CNAB file from a stream and persists all valid transactions.
    /// Returns a detailed result with imported and failed lines.
    /// </summary>
    Task<CnabImportResultDto> ImportAsync(Stream cnabStream, CancellationToken cancellationToken = default);
}
