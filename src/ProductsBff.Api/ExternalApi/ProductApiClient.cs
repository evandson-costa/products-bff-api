namespace ProductsBff.Api.ExternalApi;

using ProductsBff.Api.Models;

/// <summary>
/// Implementação MOCK de IProductApiClient.
/// Retorna dados da classe estática MockProductData em vez de fazer chamadas HTTP.
///
/// COMO SERIA EM PRODUÇÃO:
///   - Injetar IHttpClientFactory no construtor
///   - Chamar: await _httpClient.GetFromJsonAsync&lt;List&lt;Product&gt;&gt;("/api/products")
///   - Aplicar políticas de retry com Polly para erros transientes
///   - Registrar no DI com AddHttpClient&lt;IProductApiClient, ProductApiClient&gt;()
///
/// O uso de Task.FromResult é intencional: mantém a assinatura async idêntica
/// a uma chamada HTTP real. Trocar esta classe pelo cliente HTTP real exige
/// ZERO mudanças em ProductBffService ou no controller.
/// </summary>
public sealed class ProductApiClient : IProductApiClient
{
    private readonly ILogger<ProductApiClient> _logger;

    // ILogger injetado para logar chamadas ao mock durante desenvolvimento,
    // exatamente como logaríamos chamadas HTTP reais em produção.
    public ProductApiClient(ILogger<ProductApiClient> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MockClient] GetAllProductsAsync chamado — retornando {Count} produtos fixos.",
            MockProductData.Products.Count);

        // Task.FromResult envolve um valor já calculado em uma Task completada.
        // Simula fielmente o retorno awaitable de uma chamada real de HttpClient.
        return Task.FromResult(MockProductData.Products);
    }

    /// <inheritdoc />
    public Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default)
    {
        var product = MockProductData.Products.FirstOrDefault(p => p.Id == id);

        if (product is null)
            _logger.LogWarning("[MockClient] Produto {Id} não encontrado nos dados mock.", id);
        else
            _logger.LogInformation("[MockClient] Retornando produto mock {Id}: {Name}.", id, product.Name);

        // O cast para Product? é necessário porque FirstOrDefault retorna o tipo
        // não-nulável Product, mas a assinatura do método retorna Product?.
        return Task.FromResult<Product?>(product);
    }
}
