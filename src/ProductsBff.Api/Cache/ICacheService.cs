namespace ProductsBff.Api.Cache;

/// <summary>
/// Abstração genérica de cache read-aside.
///
/// DECISÃO ARQUITETURAL: não expor IDistributedCache diretamente ao serviço BFF.
/// Se o fizéssemos, a lógica de serialização JSON vazaria para dentro da camada
/// de negócio. Com essa interface, o serviço BFF trabalha com tipos fortemente
/// tipados e não sabe nada sobre bytes ou JSON.
///
/// Benefício adicional: é simples criar um stub de ICacheService em testes
/// unitários que sempre retorna null (simulando cache miss) sem precisar
/// configurar Redis.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Tenta obter um valor em cache pela chave.
    /// Retorna null em cache miss OU se o Redis estiver indisponível.
    ///
    /// ESTRATÉGIA FAIL-OPEN: erros de Redis são tratados como cache miss.
    /// A API nunca retorna 500 por causa do Redis — degradação graciosa.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Armazena um valor no cache com TTL absoluto.
    ///
    /// POR QUE TTL ABSOLUTO (e não sliding):
    ///   SlidingExpiration reseta a cada leitura, o que pode manter dados
    ///   desatualizados no cache indefinidamente sob alto tráfego.
    ///   AbsoluteExpiration garante que o cache sempre expire após `expiry`,
    ///   independentemente de quantas leituras ocorram.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Remove explicitamente uma chave do cache.
    /// Útil para invalidação manual quando um produto é atualizado.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken ct = default);
}
