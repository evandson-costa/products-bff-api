namespace ProductsBff.Api.Cache;

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

/// <summary>
/// Implementação de ICacheService usando Redis via IDistributedCache.
///
/// IDistributedCache é fornecido pelo pacote
/// Microsoft.Extensions.Caching.StackExchangeRedis e configurado no Program.cs
/// via AddStackExchangeRedisCache.
///
/// SERIALIZAÇÃO: System.Text.Json (incluso no SDK, zero dependências extras).
/// As opções são armazenadas como campo estático para não realocar a cada chamada —
/// construir JsonSerializerOptions é relativamente caro.
///
/// RESILIÊNCIA (fail-open): cada chamada ao Redis é envolto em try/catch.
/// Uma queda do Redis NUNCA derruba a API — a exceção vira um log de Warning
/// e o cache miss permite que o serviço BFF busque os dados na fonte original.
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    // Reutilizar as mesmas opções de serialização entre chamadas é uma otimização
    // importante: JsonSerializerOptions tem custo de construção não trivial.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            // IDistributedCache.GetAsync retorna byte[]? (null = cache miss)
            var bytes = await _cache.GetAsync(key, ct);

            if (bytes is null)
            {
                _logger.LogDebug("[Cache] MISS para a chave '{Key}'.", key);
                return null;
            }

            _logger.LogDebug("[Cache] HIT para a chave '{Key}'.", key);

            // Desserializa os bytes JSON de volta para o tipo fortemente tipado T.
            // System.Text.Json suporta init-only properties via construtores sintetizados.
            return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
        }
        catch (Exception ex)
        {
            // FAIL-OPEN: logamos o erro mas NÃO relançamos a exceção.
            // O chamador (ProductBffService) interpretará null como cache miss
            // e buscará os dados na API cliente normalmente.
            _logger.LogWarning(ex, "[Cache] GET falhou para '{Key}'. Tratando como cache miss.", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct = default) where T : class
    {
        try
        {
            // SerializeToUtf8Bytes é mais eficiente que Serialize para string porque
            // evita a conversão intermediária de UTF-16 para UTF-8.
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);

            var options = new DistributedCacheEntryOptions
            {
                // TTL absoluto: o cache expira exatamente após `expiry` a partir da gravação,
                // independentemente de quantas leituras ocorram nesse período.
                AbsoluteExpirationRelativeToNow = expiry
            };

            await _cache.SetAsync(key, bytes, options, ct);
            _logger.LogDebug("[Cache] SET chave '{Key}' com TTL {Expiry}.", key, expiry);
        }
        catch (Exception ex)
        {
            // FAIL-OPEN: falha na gravação significa que a próxima requisição baterá
            // na API novamente. Não ideal em produção, mas nunca causa 500.
            _logger.LogWarning(ex, "[Cache] SET falhou para '{Key}'. Dado não cacheado.", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(key, ct);
            _logger.LogDebug("[Cache] REMOVIDO chave '{Key}'.", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] REMOVE falhou para '{Key}'.", key);
        }
    }
}
