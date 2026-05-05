namespace ProductsBff.Api.Models;

/// <summary>
/// Entidade de domínio interna que representa um produto exatamente como
/// a API externa o retorna. O BFF NUNCA expõe essa classe diretamente ao
/// frontend — ela é apenas uma representação interna da fonte de dados.
///
/// DECISÃO ARQUITETURAL: separar a entidade interna (Product) do contrato
/// de frontend (ProductSummary) permite que a API externa mude seu schema
/// sem quebrar o contrato com o browser.
/// </summary>
public sealed class Product
{
    /// <summary>Identificador único vindo da API externa.</summary>
    public int Id { get; init; }

    /// <summary>Nome legível do produto.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Descrição curta do produto.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Preço em USD. Sempre positivo.</summary>
    public decimal Price { get; init; }

    /// <summary>Categoria do produto (ex: "Electronics", "Furniture").</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Quantidade em estoque conforme reportado pela API externa.
    /// O BFF usa esse valor para calcular o campo InStock no view model —
    /// o número bruto nunca chega ao frontend para não vazar dados de inventário.
    /// </summary>
    public int StockQuantity { get; init; }

    /// <summary>Timestamp ISO 8601 de criação do produto na origem.</summary>
    public DateTime CreatedAt { get; init; }
}
