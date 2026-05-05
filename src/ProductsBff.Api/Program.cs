// ─────────────────────────────────────────────────────────────────────────────
// Program.cs — Raiz de composição da aplicação
//
// Este arquivo é o único lugar onde todas as decisões de injeção de dependência
// e configuração de middleware são tomadas. Comentários explicam o PORQUÊ de
// cada decisão, não apenas o QUÊ.
// ─────────────────────────────────────────────────────────────────────────────

using ProductsBff.Api.Cache;
using ProductsBff.Api.ExternalApi;
using ProductsBff.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Controllers MVC ────────────────────────────────────────────────────────
// AddControllers registra o pipeline MVC necessário para classes [ApiController].
// NÃO adiciona Razor Pages ou Views — correto para uma API pura.
builder.Services.AddControllers();

// ── 2. OpenAPI / Swagger ──────────────────────────────────────────────────────
// Permite explorar e testar a API interativamente em /swagger/index.html
// durante desenvolvimento sem precisar de ferramenta externa.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "ProductsBff API",
        Version = "v1",
        Description =
            "Demonstração do padrão BFF (Backend for Frontend) com Redis como cache " +
            "e uma API externa simulada por dados fixos em classe estática."
    });
});

// ── 3. Redis Distributed Cache ────────────────────────────────────────────────
// AddStackExchangeRedisCache registra IDistributedCache com backend Redis.
// A connection string vem de appsettings.json (seção ConnectionStrings:Redis).
//
// abortConnect=false: a aplicação NÃO falha ao iniciar se o Redis estiver
//   fora do ar. Essencial para ambientes containerizados onde o Redis pode
//   subir alguns segundos depois da API.
//
// InstanceName: todas as chaves gravadas por este BFF recebem o prefixo
//   "ProductsBff:" no Redis físico, evitando colisões se múltiplos serviços
//   compartilharem a mesma instância Redis.
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException(
        "Connection string 'ConnectionStrings:Redis' ausente na configuração. " +
        "Adicione em appsettings.json ou via variável de ambiente CONNECTIONSTRINGS__REDIS.");

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName  = "ProductsBff:";
});

// ── 4. Serviços da Aplicação (Injeção de Dependência) ─────────────────────────
//
// EXPLICAÇÃO DOS LIFETIMES:
//
//   Transient  — nova instância a cada injeção. Para serviços leves e sem estado.
//   Scoped     — uma instância por requisição HTTP. Para serviços com estado por requisição.
//   Singleton  — uma instância para toda a vida da aplicação. Para serviços thread-safe compartilhados.
//
// ProductApiClient → Scoped
//   Em produção, envolveria um HttpClient (IHttpClientFactory cria um por escopo).
//   A versão mock é stateless, mas Scoped é o default correto para antecipar
//   a troca pelo cliente HTTP real.
//
// RedisCacheService → Scoped
//   IDistributedCache (sua dependência) é registrado como Singleton internamente
//   pelo AddStackExchangeRedisCache. Scoped é seguro aqui; nunca registre um
//   serviço Scoped como Singleton (captive dependency).
//
// ProductBffService → Scoped
//   Depende dos dois acima (ambos Scoped), portanto também deve ser Scoped.

builder.Services.AddScoped<IProductApiClient, ProductApiClient>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IProductBffService, ProductBffService>();

// ── 5. CORS (descomentável para frontend real) ────────────────────────────────
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("FrontendPolicy", policy =>
//         policy.WithOrigins("http://localhost:3000")  // URL do seu frontend
//               .AllowAnyMethod()
//               .AllowAnyHeader());
// });

// ─────────────────────────────────────────────────────────────────────────────
// Pipeline de Middleware
//
// A ORDEM IMPORTA: cada middleware chama next() para passar a requisição ao
// próximo componente. Exceções:
//   - UseExceptionHandler deve vir primeiro para capturar erros de qualquer camada
//   - UseAuthorization deve vir após UseAuthentication
// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Ferramentas de desenvolvimento ────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    // Swagger UI: acessível em https://localhost:{porta}/swagger
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductsBff API v1");
        options.RoutePrefix = "swagger";
    });
}

// Redireciona HTTP → HTTPS em todos os ambientes.
app.UseHttpsRedirection();

// Mapeia as rotas declaradas com atributos [Route] nos controllers [ApiController].
app.MapControllers();

app.Run();
