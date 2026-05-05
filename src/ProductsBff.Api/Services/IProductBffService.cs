namespace ProductsBff.Api.Services;

using ProductsBff.Api.Models;

/// <summary>
/// Contrato do serviço de orquestração BFF para produtos.
///
/// O controller conhece apenas esta interface — nunca o Redis, nunca o cliente
/// externo. Toda a lógica de "verificar cache, fallback para API, transformar
/// dados" fica encapsulada na implementação concreta ProductBffService.
/// </summary>
public interface IProductBffService
{
    /// <summary>
    /// Retorna todos os produtos como view models prontos para o frontend.
    /// Dados servidos do cache Redis quando disponíveis; caso contrário,
    /// buscados na API cliente externa e gravados no cache.
    /// </summary>
    Task<IReadOnlyList<ProductSummary>> GetAllProductsAsync(CancellationToken ct = default);

    /// <summary>
    /// Retorna um único product summary por ID.
    /// Retorna null quando o produto não existe (o controller mapeia para HTTP 404).
    /// </summary>
    Task<ProductSummary?> GetProductByIdAsync(int id, CancellationToken ct = default);
}
