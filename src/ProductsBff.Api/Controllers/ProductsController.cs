namespace ProductsBff.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using ProductsBff.Api.Models;
using ProductsBff.Api.Services;

/// <summary>
/// Ponto de entrada HTTP único para todas as requisições de produtos do frontend.
///
/// RESPONSABILIDADES DO CONTROLLER (camada fina):
///   - Mapear verbos/rotas HTTP para chamadas de serviço
///   - Mapear valores de retorno do serviço para status HTTP corretos
///   - Validar parâmetros de rota/query de forma básica
///
/// O CONTROLLER NÃO DEVE:
///   - Conter lógica de negócio
///   - Saber que existe Redis
///   - Saber que existe uma API externa
///   Tudo isso é responsabilidade de IProductBffService.
///
/// BASE CLASS: ControllerBase (não Controller) porque esta API retorna JSON,
/// não Razor Views. ControllerBase é mais leve.
/// </summary>
[ApiController]
[Route("api/[controller]")]    // resolve para: /api/products
[Produces("application/json")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductBffService _bffService;

    // Injeção de dependência por construtor: o DI do ASP.NET Core resolve
    // IProductBffService → ProductBffService automaticamente conforme
    // registrado no Program.cs.
    public ProductsController(IProductBffService bffService)
    {
        _bffService = bffService;
    }

    /// <summary>
    /// GET /api/products
    /// Retorna todos os produtos como view models prontos para o frontend.
    /// A resposta é servida do cache Redis quando disponível (TTL: 5 min).
    /// </summary>
    /// <param name="ct">CancellationToken fornecido pelo ASP.NET Core quando o cliente desconecta.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllProducts(CancellationToken ct)
    {
        var products = await _bffService.GetAllProductsAsync(ct);
        return Ok(products);
    }


    /// <summary>
    /// GET /api/products/{id}
    /// Retorna um único produto pelo ID.
    /// Retorna 404 se o produto não existir, 400 se o ID for inválido.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProductById([FromRoute] int id, CancellationToken ct)
    {
        // Guarda básica: IDs negativos ou zero são estruturalmente inválidos.
        // A constraint "{id:int}" na rota já garante que é um int; esta validação
        // complementar rejeita o domínio inválido de inteiros não-positivos.
        if (id <= 0)
            return BadRequest(new { error = "O ID do produto deve ser um inteiro positivo." });

        var product = await _bffService.GetProductByIdAsync(id, ct);

        // O serviço retorna null quando a API externa não encontrou o produto.
        // O BFF traduz isso em um HTTP 404 com corpo estruturado.
        if (product is null)
            return NotFound(new { error = $"Produto com ID {id} não encontrado." });

        return Ok(product);
    }
}
