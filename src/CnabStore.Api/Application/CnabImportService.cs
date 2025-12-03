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

    /// <inheritdoc />
    public async Task<CnabImportResultDto> ImportAsync(Stream cnabStream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(cnabStream);

        var imported = new List<CnabImportSuccessDto>();
        var failed = new List<CnabImportErrorDto>();

        var lineNumber = 0;

        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                // Empty or whitespace-only lines are ignored and not counted
                continue;
            }

            try
            {
                var dto = _parser.ParseLine(line);
                await UpsertStoreAndAddTransactionAsync(dto, cancellationToken);

                imported.Add(new CnabImportSuccessDto(lineNumber, dto));
            }
            catch (Exception ex)
            {
                failed.Add(new CnabImportErrorDto(lineNumber, ex.Message, line));
            }
        }

        // Persist all valid transactions
        await _dbContext.SaveChangesAsync(cancellationToken);

        var totalLines = imported.Count + failed.Count;

        return new CnabImportResultDto(TotalLines: totalLines,
                                       ImportedCount: imported.Count,
                                       FailedCount: failed.Count,
                                       Imported: imported,
                                       Failed: failed);
    }

    private async Task UpsertStoreAndAddTransactionAsync(TransactionDto dto, CancellationToken cancellationToken)
    {
        var store = await FindOrCreateStoreAsync(dto, cancellationToken);

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

    /// <summary>
    /// Looks for an existing store in the current change tracker first (Local),
    /// then in the database. If none is found, creates a new one and attaches it.
    /// </summary>
    private async Task<Store> FindOrCreateStoreAsync(TransactionDto dto, CancellationToken cancellationToken)
    {
        var store = _dbContext.Stores.Local.FirstOrDefault(s => s.Name == dto.StoreName &&
                                                                s.OwnerName == dto.StoreOwner);

        if (store is null)
        {
            store = await _dbContext.Stores.FirstOrDefaultAsync(s => s.Name == dto.StoreName && s.OwnerName == dto.StoreOwner,
                                                                cancellationToken);
        }

        if (store is null)
        {
            store = new Store
            {
                Name = dto.StoreName,
                OwnerName = dto.StoreOwner
            };

            _dbContext.Stores.Add(store);
        }

        return store;
    }
}
