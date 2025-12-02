using CnabStore.Api.Application.Dtos;
using CnabStore.Api.Application.Interfaces;
using CnabStore.Api.Domain;
using CnabStore.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CnabStore.Api.Application;

/// <summary>
/// Application service responsible for importing CNAB data
/// </summary>
public class CnabImportService : ICnabImportService
{
    private readonly AppDbContext _dbContext;
    private readonly ICnabLineParser _parser;

    public CnabImportService(AppDbContext dbContext, ICnabLineParser parser)
    {
        _dbContext = dbContext;
        _parser = parser;
    }

    /// <summary>
    /// Imports a CNAB file from a stream and persists all transactions.
    /// </summary>
    public async Task ImportAsync(Stream cnabStream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(cnabStream);

        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var dto = _parser.ParseLine(line);
            await UpsertStoreAndAddTransactionAsync(dto, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Finds or creates a Store for the given DTO and adds a new Transaction to it.
    /// </summary>
    private async Task UpsertStoreAndAddTransactionAsync(TransactionDto dto,
                                                         CancellationToken cancellationToken)
    {
        var store = await _dbContext.Stores.FirstOrDefaultAsync(s => s.Name == dto.StoreName && s.OwnerName == dto.StoreOwner,
                                                                cancellationToken);

        if (store is null)
        {
            store = new Store
            {
                Name = dto.StoreName,
                OwnerName = dto.StoreOwner
            };

            _dbContext.Stores.Add(store);
        }

        var transaction = new Transaction
        {
            Type = (TransactionType)dto.Type,
            OccurredAt = dto.OccurredAt,
            Value = dto.Value,
            Cpf = dto.Cpf,
            Card = dto.Card,
            Store = store
        };

        _dbContext.Transactions.Add(transaction);
    }
}
