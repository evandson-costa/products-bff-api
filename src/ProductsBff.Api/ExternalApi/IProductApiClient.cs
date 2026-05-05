namespace ProductsBff.Api.ExternalApi;

using ProductsBff.Api.Models;

/// <summary>
/// Contrato do cliente que acessa a API externa de produtos.
///
/// DECISÃO ARQUITETURAL: o serviço BFF depende DESTA interface, não da
/// implementação concreta. Isso garante:
///   1. Testabilidade — a implementação mock pode ser substituída por um stub
///      em testes de unidade sem nenhuma mudança no serviço BFF.
///   2. Evolução — quando a API externa existir de verdade, basta registrar
///      uma nova implementação no DI; nenhum outro arquivo precisa mudar.
/// </summary>
public interface IProductApiClient
{
    /// <summary>
    /// Busca todos os produtos da API externa (ou do mock).
    /// O método é async para espelhar exatamente a assinatura que um
    /// HttpClient real exigiria — sem surpresas na hora de trocar o mock.
    /// </summary>
    Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken ct = default);

    /// <summary>
    /// Busca um produto pelo ID.
    /// Retorna null quando não encontrado, simulando o comportamento de
    /// uma API real que retornaria HTTP 404.
    /// </summary>
    Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default);
}
