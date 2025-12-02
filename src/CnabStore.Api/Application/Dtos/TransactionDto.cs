namespace CnabStore.Api.Application.Dtos;

/// <summary>
/// Internal DTO representing a single CNAB transaction line after parsing.
/// This is used inside the application layer before mapping to domain entities.
/// </summary>
public sealed record TransactionDto(
    int Type,
    DateTime OccurredAt,
    decimal Value,
    string Cpf,
    string Card,
    string StoreOwner,
    string StoreName
);
