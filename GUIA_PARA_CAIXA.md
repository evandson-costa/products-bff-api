# 🏦 Guia Completo do Projeto — Do Zero ao Backend da Caixa

> Escrito como se você tivesse 10 anos, mas sem esconder nada do que importa de verdade.

---

## 🧠 Antes de tudo: o que é uma API?

Imagina que você vai ao McDonald's.

- Você **não entra na cozinha** para fazer seu lanche.
- Você fala com o **caixa** (o atendente).
- O caixa anota seu pedido, passa para a cozinha, e te entrega o lanche pronto.

**API é exatamente isso.** É o "caixa" entre você (o app, o site, o celular) e o sistema que guarda os dados. Você pede, a API busca, a API te entrega. Você nunca vê a cozinha.
 

Na Caixa Econômica Federal isso acontece o tempo todo:
- Você abre o app e vê seu saldo → seu celular chamou uma **API**
- Você faz um Pix → seu celular chamou uma **API**
- Você pede um extrato → **API**

---

## 🏗️ O que esse projeto faz?

Esse projeto é uma **API de produtos** (como um catálogo de loja). Ele demonstra como um backend bem organizado funciona na prática, usando os mesmos padrões que sistemas bancários reais usam.
 
```
Você (browser/app)
      ↓  pergunta: "quais produtos existem?"
   [API - esse projeto]
      ↓  verifica se já tem a resposta salva (Redis)
      ↓  se não tiver, busca os dados
      ↓  devolve a resposta formatada
Você recebe a lista de produtos
```

---

## 🗂️ Mapa dos arquivos (o que cada um faz)

```
ProductsBff.Api/
│
├── Controllers/
│   └── ProductsController.cs   ← A PORTA (recebe as visitas)
│
├── Services/
│   ├── IProductBffService.cs   ← O CONTRATO (o que o cérebro promete fazer)
│   └── ProductBffService.cs    ← O CÉREBRO (onde a lógica mora)
│
├── Cache/
│   ├── ICacheService.cs        ← O CONTRATO do cofre
│   └── RedisCacheService.cs    ← O COFRE (Redis)
│
├── ExternalApi/
│   ├── IProductApiClient.cs    ← O CONTRATO do fornecedor
│   ├── ProductApiClient.cs     ← O FORNECEDOR (busca os dados)
│   └── MockProductData.cs      ← DADOS FALSOS para teste
│
├── Models/
│   ├── Product.cs              ← O produto como vem da fonte (cru)
│   └── ProductSummary.cs       ← O produto como o app recebe (pronto)
│
├── Program.cs                  ← O MAESTRO (monta tudo)
├── appsettings.json            ← AS CONFIGURAÇÕES
└── docker-compose.yml          ← A RECEITA do Redis
```

---

## 🎭 Conceito 1: BFF — Backend for Frontend

### A analogia do garçom especializado

Imagina um restaurante chique que tem vários tipos de cliente:
- Mesa de família com crianças
- Casal em jantar romântico
- Executivos em reunião de negócios

Se você mandar **um garçom genérico** para todos, ele vai servir o mesmo cardápio para todo mundo. Mas e se você tiver **um garçom especializado para cada tipo de mesa**? O garçom das crianças já sabe trazer o guardanapo colorido e o suco antes de pedir. O garçom dos executivos traz o vinho certo.

**BFF = Backend for Frontend = Garçom especializado.**

Em vez de ter uma API gigante que serve dados brutos para todo mundo, o BFF prepara os dados **exatamente no formato que aquela tela específica precisa**.

### No código:

```
API EXTERNA (cozinha)          BFF (garçom)           FRONTEND (cliente)
Product {                  →   ProductSummary {    →   Tela do app
  Id: 1                          Id: 1
  Name: "Headphone"              Name: "Headphone"
  Price: 299.99                  Price: 299.99
  StockQuantity: 42    ←transforma→ InStock: true   ← o app não precisa saber a quantidade
  CreatedAt: 2024-01-15          PriceFormatted: "$299.99" ← já formatado!
}                              }
```

O app **nunca viu** que tinha 42 unidades. Ele só sabe que tem estoque. Isso é segurança e simplificação ao mesmo tempo.

### Por que isso importa na Caixa?

A Caixa tem **múltiplos canais**: app mobile, internet banking, caixas eletrônicos, correspondentes bancários. Cada canal precisa de dados ligeiramente diferentes. O BFF resolve isso sem duplicar a lógica de negócio.

---

## 🎯 Conceito 2: Camadas (Layers)

### A analogia do prédio

Imagina um prédio de 3 andares:

```
┌─────────────────────────┐
│  3º ANDAR: Controller   │  ← Recepção. Atende quem chega, encaminha para o andar certo.
├─────────────────────────┤
│  2º ANDAR: Service      │  ← Gerência. Pensa, decide, coordena.
├─────────────────────────┤
│  1º ANDAR: Cache/Client │  ← Almoxarifado. Guarda e busca os dados.
└─────────────────────────┘
```

**Regra de ouro:** cada andar só conversa com o andar de baixo. A recepção não vai ao almoxarifado sozinha. Ela pede para a gerência, que pede para o almoxarifado.

### No código:

**Controller (Recepção)** — `ProductsController.cs`
```csharp
// Alguém chegou pedindo: GET /api/products/1
// A recepção anota (id = 1) e encaminha para o serviço
var product = await _bffService.GetProductByIdAsync(id, ct);

// Se o serviço disse "não existe" → responde 404 (Não Encontrado)
if (product is null)
    return NotFound(new { error = $"Produto com ID {id} não encontrado." });

// Se encontrou → responde 200 (OK) com o produto
return Ok(product);
```

O Controller **não sabe** que existe Redis. Não sabe que existe banco de dados. Ele só recebe, encaminha, e devolve a resposta.

**Service (Gerência)** — `ProductBffService.cs`
```csharp
// 1. Tem no cofre (Redis)?
var cached = await _cache.GetAsync<ProductSummary>(cacheKey, ct);
if (cached is not null) return cached; // sim! devolve rápido

// 2. Não tem. Busca na fonte.
var product = await _apiClient.GetProductByIdAsync(id, ct);

// 3. Transforma e guarda no cofre para a próxima vez
var summary = MapToSummary(product);
await _cache.SetAsync(cacheKey, summary, CacheTtl, ct);

return summary;
```

---

## 🔌 Conceito 3: Interface (Contrato)

### A analogia da tomada

Toda tomada brasileira tem o mesmo formato (ABNT NBR 14136). Por isso:
- Você pode plugar **qualquer** carregador nela
- O carregador não sabe se a energia veio de hidrelétrica, solar, ou eólica
- Se amanhã a cidade trocar a fonte de energia, **seu carregador continua funcionando**

**Interface = tomada padronizada.**

### No código:

```csharp
// O CONTRATO (a tomada)
public interface IProductApiClient
{
    Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken ct = default);
    Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default);
}

// IMPLEMENTAÇÃO ATUAL (energia da hidrelétrica = dados falsos/mock)
public sealed class ProductApiClient : IProductApiClient { ... }

// IMPLEMENTAÇÃO FUTURA (energia solar = API real da Caixa)
public sealed class RealProductApiClient : IProductApiClient { ... }
```

O Service só conhece `IProductApiClient`. Amanhã você troca a implementação e **nenhuma linha do Service muda**.

### Por que isso importa na Caixa?

Sistemas bancários trocam fornecedores, trocam tecnologias, integram sistemas legados. Com interfaces bem definidas, você troca a implementação sem derrubar o resto do sistema. É a diferença entre reformar um cômodo e demolir o prédio.

---

## 💉 Conceito 4: Injeção de Dependência (DI)

### A analogia do hospital

Um médico precisa de seringas, luvas, e estetoscópio para trabalhar. Ele **não vai à farmácia buscar** cada instrumento antes de cada consulta. O hospital prepara o kit e **entrega ao médico** quando ele chega.

**Injeção de Dependência = o hospital que entrega o kit.**

### No código:

**Registrando (Program.cs) — o hospital organiza os kits:**
```csharp
builder.Services.AddScoped<IProductApiClient, ProductApiClient>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IProductBffService, ProductBffService>();
```

**Recebendo (Controller) — o médico recebe o kit pronto:**
```csharp
public ProductsController(IProductBffService bffService)
{
    _bffService = bffService; // ASP.NET entregou, não precisei buscar
}
```

O Controller nunca escreveu `new ProductBffService(...)`. Alguém entregou pronto.

### Os 3 tipos de "kit" (Lifetimes):

| Tipo | Analogia | Quando usar |
|------|----------|-------------|
| **Transient** | Papel descartável | Novo a cada uso. Para coisas leves e sem estado. |
| **Scoped** | Luvas por cirurgia | Uma por requisição HTTP. Descarta ao fim de cada pedido. |
| **Singleton** | Estetoscópio | Um para sempre. Compartilhado por todos. |

**Regra de ouro:** nunca injete um Scoped dentro de um Singleton. É como guardar luvas usadas no estetoscópio permanente.

---

## ⚡ Conceito 5: Cache com Redis

### A analogia da cola de estudar

Na véspera da prova, você estuda 5 horas e anota os pontos principais em um post-it. Na prova, você consulta o post-it em 5 segundos — não relê o livro todo.

**Cache = o post-it. Redis = a caixinha de post-its.**

### O fluxo Cache-Aside (usado aqui):

```
Pergunta chega
      ↓
Tem no Redis? ──SIM──→ Devolve em milissegundos ✅
      ↓ NÃO
Vai na fonte (API externa)
      ↓
Guarda no Redis com prazo de validade (TTL = 5 minutos)
      ↓
Devolve a resposta
```

### No código:

```csharp
// Chave única para cada dado cacheado
private const string AllProductsCacheKey = "bff:products:all";
private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

// 1. Pergunta ao Redis
var cached = await _cache.GetAsync<List<ProductSummary>>(AllProductsCacheKey, ct);
if (cached is not null) return cached; // post-it encontrado!

// 2. Não tinha. Busca de verdade.
var products = await _apiClient.GetAllProductsAsync(ct);
var summaries = products.Select(MapToSummary).ToList();

// 3. Cola o post-it por 5 minutos
await _cache.SetAsync(AllProductsCacheKey, summaries, CacheTtl, ct);
```

### Fail-Open (degradação graciosa):

```csharp
try
{
    var bytes = await _cache.GetAsync(key, ct);
    // ...
}
catch (Exception ex)
{
    // Redis caiu? Tudo bem. Tratamos como cache miss.
    // A API CONTINUA FUNCIONANDO, só um pouco mais devagar.
    _logger.LogWarning(ex, "Cache GET falhou. Tratando como miss.");
    return null;
}
```

**A API nunca cai por causa do Redis.** Ela degrada graciosamente: funciona mais devagar, mas funciona.

### Por que isso é crítico na Caixa?

A Caixa tem **mais de 150 milhões de clientes**. Se 1 milhão de pessoas abrissem o app ao mesmo tempo sem cache, o banco de dados explodiria. O Redis é o que faz sistemas bancários aguentarem picos (Black Friday, pagamento do FGTS, etc).

---

## 🌐 Conceito 6: HTTP e Verbos

### A analogia do correio

Quando você manda uma carta, você coloca na frente o **tipo de operação**:
- 📬 Entregar (GET — pegar algo)
- 📝 Registrar (POST — criar algo novo)
- ✏️ Corrigir (PUT/PATCH — atualizar)
- 🗑️ Cancelar (DELETE — apagar)

### No código:

```csharp
[HttpGet]                    // GET /api/products → lista todos
public async Task<IActionResult> GetAllProducts(CancellationToken ct) { ... }

[HttpGet("{id:int}")]        // GET /api/products/1 → busca o produto 1
public async Task<IActionResult> GetProductById([FromRoute] int id, CancellationToken ct) { ... }
```

### Códigos de resposta HTTP (a linguagem do correio):

| Código | Significado | Analogia |
|--------|-------------|----------|
| **200 OK** | Deu certo | ✅ Entregue com sucesso |
| **400 Bad Request** | Você pediu errado | ✏️ Endereço ilegível |
| **404 Not Found** | Não existe | 🔍 Destinatário não encontrado |
| **500 Internal Server Error** | O servidor quebrou | 💥 Incêndio nos Correios |

```csharp
if (id <= 0)
    return BadRequest(...);   // 400 — "ID tem que ser positivo"

if (product is null)
    return NotFound(...);     // 404 — "Produto não existe"

return Ok(product);           // 200 — "Aqui está!"
```

---

## 📦 Conceito 7: Docker e Containers

### A analogia da marmita

Você quer comer o almoço da sua mãe no trabalho. Se você levar os ingredientes separados (arroz num saco, feijão em outro, carne num pote), vai ser uma bagunça. Se levar a **marmita pronta**, basta abrir e comer.

**Container Docker = marmita.** Tudo o que o programa precisa para rodar (sistema operacional, dependências, configurações) já está dentro. Não importa se o computador é Windows, Mac, ou Linux — a marmita funciona em qualquer micro-ondas.

### O docker-compose.yml desse projeto:

```yaml
services:
  redis:
    image: redis:7-alpine     # "Marmita de Redis, versão 7, tamanho mini (alpine)"
    container_name: productsbff-redis
    ports:
      - "6379:6379"           # Porta do container:Porta do seu computador
    restart: unless-stopped   # Se cair, levanta sozinho
```

**`docker compose up -d`** = "Aquece a marmita em segundo plano e deixa pronta."

### Por que isso importa na Caixa?

Em um banco, o ambiente de produção precisa ser **idêntico** ao ambiente de desenvolvimento. Com Docker, o que funciona na sua máquina funciona no servidor. Sem surpresas de "na minha máquina funcionava".

---

## ⏳ Conceito 8: Async/Await

### A analogia do restaurante

Você pede um prato que demora 20 minutos. O garçom **não fica parado na mesa esperando**. Ele vai atender outras mesas. Quando o prato ficar pronto, a cozinha avisa e ele entrega.

**Async/Await = o garçom que não fica parado esperando.**

### No código:

```csharp
// SEM async (garçom parado esperando — ruim):
var products = _apiClient.GetAllProducts(); // trava a thread aqui por 2 segundos
// nenhuma outra requisição pode ser atendida durante esses 2 segundos

// COM async (garçom livre — bom):
var products = await _apiClient.GetAllProductsAsync(ct);
// enquanto a API externa responde, o servidor atende outras 1000 requisições
```

### CancellationToken — o cliente desistiu, para tudo:

```csharp
public async Task<IActionResult> GetAllProducts(CancellationToken ct)
```

Se o usuário fechar o app no meio da requisição, o `CancellationToken` avisa o servidor. O servidor para tudo e não desperdiça recursos. Em um sistema com milhões de usuários, isso economiza muito processamento.

---

## 🔄 Conceito 9: Injeção de Dependência — os Lifetimes na prática

Já vimos os lifetimes na teoria. Veja como o projeto os usa:

```csharp
// Program.cs — onde tudo é registrado
builder.Services.AddScoped<IProductApiClient, ProductApiClient>();
// Scoped: uma instância por requisição HTTP
// Motivo: em produção usaria HttpClient (um por requisição = seguro)

builder.Services.AddScoped<ICacheService, RedisCacheService>();
// Scoped: também uma por requisição
// Motivo: sua dependência (IDistributedCache) é Singleton internamente

builder.Services.AddScoped<IProductBffService, ProductBffService>();
// Scoped: depende dos dois acima, então também deve ser Scoped
```

**Regra prática:** se você não sabe qual usar, comece com Scoped. É seguro na maioria dos casos.

---

## 🗺️ Conceito 10: O fluxo completo de uma requisição

Vamos seguir uma requisição do início ao fim:

```
Browser/App: GET /api/products/3
                    │
                    ▼
    ┌─────────────────────────────┐
    │     ProductsController      │
    │  1. Recebe id = 3           │
    │  2. Valida: id > 0? Sim ✓   │
    │  3. Chama bffService        │
    └────────────┬────────────────┘
                 │
                 ▼
    ┌─────────────────────────────┐
    │      ProductBffService      │
    │  4. Monta chave "bff:products:3"    │
    │  5. Pergunta ao Redis               │
    └────────────┬────────────────┘
                 │
        ┌────────┴────────┐
        │                 │
     Cache HIT         Cache MISS
        │                 │
        ▼                 ▼
  Retorna do         Chama ApiClient
  Redis (rápido)          │
        │                 ▼
        │         ProductApiClient
        │         6. Busca nos dados mock
        │         7. Encontrou produto 3
        │                 │
        │                 ▼
        │         ProductBffService
        │         8. Transforma Product → ProductSummary
        │            (StockQuantity 15 → InStock: true)
        │         9. Salva no Redis por 5 minutos
        │                 │
        └────────┬────────┘
                 │
                 ▼
    ┌─────────────────────────────┐
    │     ProductsController      │
    │  10. Recebe ProductSummary  │
    │  11. Retorna Ok(summary)    │
    └─────────────────────────────┘
                 │
                 ▼
    Browser/App recebe:
    HTTP 200 OK
    {
      "id": 3,
      "name": "Cadeira Ergonômica",
      "priceFormatted": "$449.00",
      "inStock": true
    }
```

---

## 🏦 Como isso se aplica à Caixa Econômica Federal

Tudo que você viu aqui é a base do que você vai encontrar. Só em escala muito maior e com mais responsabilidade.

### Padrões que você vai reencontrar:

| O que viu aqui | Como aparece na Caixa |
|---|---|
| BFF Pattern | APIs separadas por canal: app, internet banking, caixa eletrônico |
| Redis Cache | Cache de saldo, cache de limites de crédito, cache de taxas |
| Interfaces + DI | Integração com sistemas legados (COBOL) via interfaces |
| Camadas (Controller/Service) | Separação entre regras de negócio e exposição HTTP |
| Docker | Ambientes de homologação e produção containerizados |
| Async/Await | Nenhuma thread pode ficar bloqueada num banco com 150M de clientes |
| CancellationToken | Transações abandonadas devem ser canceladas, não desperdiçadas |
| Fail-Open (cache) | Degradação graciosa: se o cache cair, o sistema continua operando |

### Conceitos extras que você vai estudar para a Caixa:

**1. Autenticação e Autorização (você vai usar muito)**
- JWT Tokens — o crachá digital. Você loga uma vez e recebe um token assinado.
- OAuth 2.0 — o padrão que o governo usa (o Gov.br usa OAuth)
- Cada endpoint da Caixa exige saber: "quem é essa pessoa e o que ela pode fazer?"

**2. Mensageria (filas)**
- Kafka, RabbitMQ — para operações que não precisam de resposta imediata
- Pix usa fila: você envia, o sistema processa, você recebe a confirmação
- Nunca trava o usuário esperando uma operação longa

**3. Resiliência**
- Retry com backoff — tenta de novo, mas espera um pouco mais a cada tentativa
- Circuit Breaker — se a API externa falhar 5 vezes seguidas, para de chamar por 30 segundos
- Timeout — nunca espera para sempre

**4. Observabilidade**
- Logs estruturados (como esse projeto já faz)
- Métricas — quantas requisições por segundo, tempo de resposta médio
- Tracing distribuído — rastrear uma operação que passa por 10 microserviços

**5. LGPD e Segurança**
- Dados bancários são dados sensíveis por lei
- Nunca logar senha, número de conta completo, ou dados pessoais em texto puro
- Criptografia em trânsito (HTTPS) e em repouso (banco de dados criptografado)

---

## 📋 Resumo de uma linha por conceito

| Conceito | Em uma frase |
|----------|-------------|
| **API** | O caixa do McDonald's: você pede, ele entrega, você não vê a cozinha |
| **BFF** | Um garçom especializado que prepara os dados no formato exato que cada tela precisa |
| **Camadas** | Cada parte do sistema tem UMA responsabilidade e não se mete nas outras |
| **Interface** | Um contrato: "prometo ter esses métodos" — quem cumpre pode ser trocado livremente |
| **DI** | O hospital entrega o kit ao médico — o médico não vai buscar nada |
| **Redis/Cache** | O post-it: resposta que já foi buscada, guardada para ser reutilizada rápido |
| **Docker** | A marmita: o programa e tudo que ele precisa, portátil, roda em qualquer lugar |
| **Async/Await** | O garçom que não fica parado esperando o prato — atende outras mesas |
| **HTTP Verbos** | GET=buscar, POST=criar, PUT=atualizar, DELETE=apagar |
| **Fail-Open** | Se o Redis cair, a API continua — devagar, mas continua |

---

*Projeto: ProductsBff.Api — .NET 10, Redis, Padrão BFF*
*Destino: Base de conhecimento para atuação em backend bancário*
