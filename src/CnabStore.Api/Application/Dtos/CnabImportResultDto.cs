namespace CnabStore.Api.Application.Dtos;

/// <summary>
/// Aggregated result of a CNAB import operation.
/// </summary>
public sealed record CnabImportResultDto(int TotalLines,
                                         int ImportedCount,
                                         int FailedCount,
                                         IReadOnlyList<CnabImportSuccessDto> Imported,
                                         IReadOnlyList<CnabImportErrorDto> Failed);
