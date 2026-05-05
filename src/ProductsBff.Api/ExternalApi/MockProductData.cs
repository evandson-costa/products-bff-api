namespace ProductsBff.Api.ExternalApi;

using ProductsBff.Api.Models;

/// <summary>
/// Repositório estático de dados que simula o banco de dados de uma API
/// externa de produtos que não existe de fato.
///
/// POR QUE EXISTE ESSA CLASSE SEPARADA:
///   Em vez de inline no ProductApiClient, os dados ficam aqui por três motivos:
///   1. Facilita trocar por um fixture JSON em testes
///   2. O ProductApiClient pode ser lido sem scrollar por dezenas de linhas de dados
///   3. Fica claro que os dados são um artefato de simulação, não lógica de negócio
///
/// EM PRODUÇÃO: esta classe seria substituída por um HttpClient que chama
/// a URL real da API externa. A interface IProductApiClient permanece idêntica.
/// </summary>
internal static class MockProductData
{
    /// <summary>
    /// Lista somente-leitura de produtos. IReadOnlyList impede mutação acidental
    /// em runtime. Datas fixas garantem resultados determinísticos em testes.
    /// </summary>
    public static readonly IReadOnlyList<Product> Products = new List<Product>
    {
        new()
        {
            Id = 1,
            Name = "Headphone Sem Fio com Cancelamento de Ruído",
            Description = "Over-ear com 30h de bateria e cancelamento ativo de ruído.",
            Price = 299.99m,
            Category = "Electronics",
            StockQuantity = 42, // em estoque → InStock = true no view model
            CreatedAt = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = 2,
            Name = "Teclado Mecânico TKL",
            Description = "Layout tenkeyless com switches Cherry MX Blue e RGB por tecla.",
            Price = 129.99m,
            Category = "Electronics",
            StockQuantity = 0, // sem estoque → InStock = false (demonstra a transformação)
            CreatedAt = new DateTime(2024, 2, 3, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = 3,
            Name = "Cadeira de Escritório Ergonômica",
            Description = "Suporte lombar, apoio de braço ajustável e encosto em tela.",
            Price = 449.00m,
            Category = "Furniture",
            StockQuantity = 15,
            CreatedAt = new DateTime(2024, 3, 10, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = 4,
            Name = "Hub USB-C 7-em-1",
            Description = "HDMI 4K, 3x USB-A, leitor SD e passagem de energia 100W.",
            Price = 49.99m,
            Category = "Electronics",
            StockQuantity = 200,
            CreatedAt = new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = 5,
            Name = "Tapete Anti-Fadiga para Bancada",
            Description = "Tapete anti-fadiga com bordas chanfradas e 2cm de espessura.",
            Price = 79.95m,
            Category = "Furniture",
            StockQuantity = 88,
            CreatedAt = new DateTime(2024, 4, 22, 0, 0, 0, DateTimeKind.Utc)
        },
    };
}
