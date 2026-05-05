namespace ProductsBff.Api.Models;

/// <summary>
/// View model orientado ao frontend — o contrato público da API BFF.
///
/// DECISÃO ARQUITETURAL (BFF Pattern): este objeto é desenhado para a necessidade
/// exata da tela de produtos do frontend. O BFF adiciona campos computados
/// (InStock, PriceFormatted) que o browser usaria diretamente, eliminando
/// lógica duplicada no cliente.
///
/// Diferenças em relação a Product (entidade interna):
///   - StockQuantity removido → substituído por InStock (bool)
///   - Price mantido (para cálculos no cliente) + PriceFormatted adicionado
///   - CreatedAt removido (irrelevante para a listagem de produtos)
/// </summary>
public sealed class ProductSummary
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Preço pré-formatado (ex: "$19.99") gerado pelo BFF com cultura en-US.
    /// O frontend não precisa de lógica de formatação de moeda.
    /// </summary>
    public string PriceFormatted { get; init; } = string.Empty;

    /// <summary>Preço decimal bruto para cálculos no cliente (ex: carrinho).</summary>
    public decimal Price { get; init; }

    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Derivado pelo BFF: StockQuantity > 0.
    /// Encapsula a regra de negócio e evita vazar números de inventário ao browser.
    /// </summary>
    public bool InStock { get; init; }
}
