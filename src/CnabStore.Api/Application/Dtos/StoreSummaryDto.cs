namespace CnabStore.Api.Application.Dtos;

/// <summary>
/// DTO used to expose aggregated information per store.
/// </summary>
public sealed record StoreSummaryDto(
    int StoreId,
    string StoreName,
    string OwnerName,
    decimal TotalBalance
);
