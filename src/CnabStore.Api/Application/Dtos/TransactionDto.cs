namespace CnabStore.Api.Application.Dtos;

/// <summary>
/// Internal DTO representing a single CNAB transaction line after parsing.
/// </summary>
public sealed record TransactionDto(int Type,
                                    DateTime OccurredAt,
                                    decimal Value,
                                    string Cpf,
                                    string Card,
                                    string StoreOwner,
                                    string StoreName);
