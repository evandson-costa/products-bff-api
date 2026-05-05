namespace ProductsBff.Api.Services;

using ProductsBff.Api.Cache;
using ProductsBff.Api.ExternalApi;
using ProductsBff.Api.Models;

/// <summary>
/// Serviço de orquestração central do BFF. É aqui que o padrão BFF fica mais
/// evidente: este serviço é o único ponto que conhece TODAS as peças:
///   - O cache (Redis)
///   - O cliente externo (mock ou HTTP)
///   - A regra de transformação (Product → ProductSummary)
///
/// FLUXO (cache-aside / lazy population):
///   1. Verificar Redis → cache HIT: retornar imediatamente
///   2. Cache MISS: chamar o cliente externo
///   3. Transformar os dados brutos em view model
///   4. Gravar no Redis para as próximas requisições
///   5. Retornar ao controller
///
/// ESCOPO DE DEPENDÊNCIAS:
///   Controller → IProductBffService → ICacheService + IProductApiClient
///   O controller não sabe da existência do Redis ou do cliente externo.
/// </summary>
public sealed class ProductBffService : IProductBffService
{
    private readonly IProductApiClient _apiClient;
    private readonly ICacheService _cache;
    private readonly ILogger<ProductBffService> _logger;

    // Chaves de cache centralizadas aqui para evitar strings mágicas espalhadas.
    // Prefixo "bff:" separa as chaves deste BFF caso outros serviços compartilhem
    // a mesma instância Redis. O InstanceName em Program.cs adiciona outro prefixo
    // no nível da instância (ex: "ProductsBff:bff:products:all").
    private const string AllProductsCacheKey = "bff:products:all";
    private const string ProductCacheKeyPrefix = "bff:products:";

    // TTL de 5 minutos. Em produção, viria de IOptions<CacheSettings> para
    // permitir ajuste sem recompilação.
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public ProductBffService(
        IProductApiClient apiClient,
        ICacheService cache,
        ILogger<ProductBffService> logger)
    {
        _apiClient = apiClient;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductSummary>> GetAllProductsAsync(CancellationToken ct = default)
    {
        // PASSO 1: Verificar o cache.
        // Cacheamos a lista de ProductSummary já transformada, não a de Product.
        // Motivo: a transformação é barata; cachear o resultado final significa
        // que o caminho quente (cache HIT) faz zero trabalho de mapeamento.
        var cached = await _cache.GetAsync<List<ProductSummary>>(AllProductsCacheKey, ct);
        if (cached is not null)
        {
            _logger.LogInformation("[BffService] {Count} produtos servidos do cache.", cached.Count);
            return cached;
        }

        // PASSO 2: Cache miss — chamar a API cliente (mock neste exemplo).
        _logger.LogInformation("[BffService] Cache miss para todos os produtos. Chamando cliente externo.");
        var products = await _apiClient.GetAllProductsAsync(ct);

        // PASSO 3: Transformar entidades internas em view models para o frontend.
        // Select + ToList materializa a enumeração aqui; não queremos enumerar
        // duas vezes (uma para cachear, outra para retornar).
        var summaries = products.Select(MapToSummary).ToList();

        // PASSO 4: Popular o cache para as próximas requisições.
        await _cache.SetAsync(AllProductsCacheKey, summaries, CacheTtl, ct);

        _logger.LogInformation("[BffService] {Count} produtos retornados do cliente externo e cacheados.", summaries.Count);
        return summaries;
    }

    /// <inheritdoc />
    public async Task<ProductSummary?> GetProductByIdAsync(int id, CancellationToken ct = default)
    {
        // Chave por produto: "bff:products:1", "bff:products:2", etc.
        var cacheKey = $"{ProductCacheKeyPrefix}{id}";

        // PASSO 1: Verificar cache por produto individual.
        var cached = await _cache.GetAsync<ProductSummary>(cacheKey, ct);
        if (cached is not null)
        {
            _logger.LogInformation("[BffService] Produto {Id} servido do cache.", id);
            return cached;
        }

        // PASSO 2: Cache miss — buscar na fonte.
        _logger.LogInformation("[BffService] Cache miss para produto {Id}. Chamando cliente externo.", id);
        var product = await _apiClient.GetProductByIdAsync(id, ct);

        if (product is null)
        {
            // NÃO cacheamos null/not-found. Uma janela curta em que um produto
            // recém-criado retorna 404 é aceitável; cachear null por 5 minutos
            // após a criação seria pior para a experiência do usuário.
            return null;
        }

        // PASSO 3: Transformar e cachear.
        var summary = MapToSummary(product);
        await _cache.SetAsync(cacheKey, summary, CacheTtl, ct);

        return summary;
    }

    /// <summary>
    /// Transforma uma entidade interna Product em um view model ProductSummary.
    ///
    /// TRANSFORMAÇÕES APLICADAS:
    ///   - Price → PriceFormatted: formatação de moeda feita no servidor (cultura en-US)
    ///   - StockQuantity → InStock: regra de negócio encapsulada aqui
    ///
    /// Função pura (sem efeitos colaterais, sem I/O) — fácil de testar em isolamento.
    /// Em um BFF maior poderia ser extraída para uma classe Mapper dedicada.
    /// </summary>
    private static ProductSummary MapToSummary(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        PriceFormatted = p.Price.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US")),
        Category = p.Category,
        InStock = p.StockQuantity > 0
    };
}
