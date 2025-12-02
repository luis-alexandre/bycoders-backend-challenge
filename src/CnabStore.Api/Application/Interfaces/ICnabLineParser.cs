using CnabStore.Api.Application.Dtos;

namespace CnabStore.Api.Application.Interfaces;

/// <summary>
/// Abstraction for parsing a single CNAB line into a TransactionDto.
/// </summary>
public interface ICnabLineParser
{
    TransactionDto ParseLine(string line);
}
