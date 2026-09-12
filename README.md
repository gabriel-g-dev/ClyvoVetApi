# ClyvoVet API

API REST em ASP.NET Core (.NET 9) para gestão de histórico clínico veterinário.
FIAP Challenge 2026 — Clyvo VET.

| Integrante | RM |
|---|---|
| Gabriel Garcia | 563298 |
| Andre Bellandi | 564662 |
| Vitor Augusto | 564227 |

---

## Descrição da solução

Centraliza o histórico de saúde de pets em um único backend: tutores, animais, funcionários, consultas, vacinas e medicamentos prescritos. Sete entidades relacionais em Oracle, acessadas por Entity Framework Core com mapeamento Fluent API.

Além do CRUD, a API expõe:

- histórico longitudinal por pet — consultas e vacinas em uma única requisição;
- score de saúde preventiva (0–100) calculado a partir do histórico do animal, com recomendações derivadas;
- serviço em segundo plano (`AlertScheduler`) que varre vacinas vencidas e próximas do vencimento.

Arquitetura em camadas: Controllers → Services → Repositories → EF Core → Oracle. Services dependem de interfaces de repositório, o que permite testá-los sem banco. Exceções de domínio (`NotFoundException`, `BusinessException`) são traduzidas em RFC 7807 ProblemDetails por um `IExceptionHandler` global.

A solution contém três projetos:

```
ClyvoVetApi                      API
ClyvoVetApi.Tests.Unit           131 testes — xUnit + Moq
ClyvoVetApi.Tests.Integration     45 testes — WebApplicationFactory + SQLite in-memory
```

### Stack

| Componente | Versão |
|---|---|
| .NET / ASP.NET Core | 9.0 (`net9.0`) |
| Entity Framework Core | 9.0.4 |
| Oracle.EntityFrameworkCore | 9.23.60 |
| Serilog.AspNetCore | 9.0.0 |
| OpenTelemetry | 1.17.0 |
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.0 |
| Scalar.AspNetCore | 2.14.14 |
| xUnit / Moq | 2.9.3 / 4.20.72 |
| Microsoft.AspNetCore.Mvc.Testing | 9.0.0 |

Requer .NET 9 instalado (SDK 9.0.x ou ASP.NET Core Runtime 9.0.x). O projeto tem como alvo `net9.0`: apenas com .NET 8 ou .NET 10 o `dotnet build` funciona, mas `dotnet run` e `dotnet test` falham.

---

## Benefícios para o negócio

**Atendimento preventivo em vez de reativo.** O score de saúde e a varredura de vacinas pendentes permitem que a clínica identifique animais em risco antes da queixa clínica, transformando consulta de emergência em acompanhamento programado.

**Histórico unificado.** Cada atendimento passa a compor um prontuário contínuo. O veterinário recebe consultas anteriores, vacinas aplicadas e medicações prescritas em uma requisição, sem depender da memória do tutor.

**Redução de faltas e de vacinas vencidas.** A rotina de alertas identifica vencimentos, viabilizando contato ativo com o tutor — receita recorrente que hoje se perde por esquecimento.

**Base para integração.** Sendo REST com contrato OpenAPI, o mesmo backend atende aplicativo do tutor, sistema da clínica e futuros parceiros sem reescrita.

**Operação observável.** Health checks, log estruturado com correlação e métricas de latência e erro permitem detectar degradação antes que o usuário reclame, requisito para operar em nuvem com SLA.

---

## Banco de dados

Oracle. Tabelas: `DONOS`, `FUNCIONARIOS`, `PETS`, `CONSULTAS`, `MEDICAMENTOS`, `CONSULTAS_MEDICAMENTOS`, `VACINAS`. Mapeamento em `Data/AppDbContext.cs`.

O DDL completo está em **`script_bd.sql`** na raiz — chaves primárias, seis chaves estrangeiras com `ON DELETE CASCADE`, check constraints de status e índices únicos de e-mail. Gerado a partir do modelo do EF Core:

```bash
dotnet ef dbcontext script --project ClyvoVetApi.csproj -o script_bd.sql
```

`dbcontext script` lê o `AppDbContext` e não abre conexão. `Data/AppDbContextFactory.cs` implementa `IDesignTimeDbContextFactory`, permitindo rodar o comando sem credencial configurada.

Executar o script:

```bash
sqlplus usuario/senha@host:1521/servico @script_bd.sql
```

Não execute comandos de migration neste projeto. Os arquivos em `Migrations/` descrevem uma modelagem anterior (`TUTORES`, `CLINICAS`, `VETERINARIOS`) e não correspondem ao `AppDbContext` atual.

---

## Variáveis de ambiente

Nenhum segredo é versionado. O `appsettings.json` não contém connection string nem chaves de autenticação.

| Variável | Obrigatória | Descrição |
|---|---|---|
| `ConnectionStrings__OracleConnection` | Sim | Conexão com o Oracle. Sem ela a aplicação não inicia. |
| `Auth__Username` | Sim | Usuário aceito no login. |
| `Auth__Password` | Sim | Senha correspondente. |
| `Auth__SigningKey` | Sim | Chave de assinatura do JWT, mínimo 32 caracteres. |
| `Auth__Issuer` | Não | Emissor do token. Padrão `ClyvoVetApi`. |
| `Auth__Audience` | Não | Audiência do token. Padrão `ClyvoVetApiClients`. |
| `Auth__ExpirationMinutes` | Não | Validade do token. Padrão 60. |
| `OpenTelemetry__OtlpEndpoint` | Não | Destino OTLP. Vazio desabilita a exportação. |
| `OpenTelemetry__ConsoleExporter` | Não | `true` imprime traces e métricas no console. Já vem `true` em Development. |
| `ASPNETCORE_ENVIRONMENT` | Não | `Development`, `Production` ou `Testing`. |

Comportamento quando ausentes:

- `ConnectionStrings__OracleConnection` — a inicialização falha com `InvalidOperationException` nomeando a variável, em vez de quebrar na primeira requisição ao banco. O ambiente `Testing` fica fora dessa checagem, porque os testes de integração substituem o provider por SQLite.
- `Auth__Username` / `Auth__Password` — o login sempre responde 401.
- `Auth__SigningKey` — uma chave aleatória é gerada na inicialização, com Warning no log. A API sobe, mas os tokens deixam de valer após restart.

`.env.example` lista as mesmas variáveis. Copie para `.env` (ignorado pelo Git) ao usar Docker Compose.

---

## Como executar

### CLI

```bash
git clone https://github.com/gabriel-g-dev/ClyvoVetApi.git
cd ClyvoVetApi

dotnet restore
dotnet build

export ConnectionStrings__OracleConnection="User Id=RM;Password=SENHA;Data Source=oracle.fiap.com.br:1521/ORCL"
export Auth__Username="clyvovet"
export Auth__Password="SENHA_DA_API"
export Auth__SigningKey="CHAVE_COM_PELO_MENOS_32_CARACTERES"

dotnet run
```

PowerShell usa `$env:NOME = "valor"` no lugar de `export`.

A API sobe em `http://localhost:5109`. Documentação OpenAPI em `http://localhost:5109/scalar/v1`.

### Docker Compose

```bash
cp .env.example .env    # preencha os valores
docker compose up -d --build
```

Sobe Oracle XE (1521) e a API (8080). O Compose falha com mensagem explícita se alguma variável do `.env` estiver faltando.

A imagem usa build multi-stage (`sdk:9.0` → `aspnet:9.0`), porta não privilegiada 8080 e executa como o usuário `app` (UID 1654, sem privilégios). O diretório `/app/logs`, onde o Serilog grava, tem a posse transferida para esse usuário antes do `USER app`.

### Endpoints

Autenticação por JWT Bearer. `POST /api/auth/login` devolve o token. Escritas (`POST`, `PUT`, `DELETE`) exigem token; leituras e health checks são públicos.

| Recurso | Rota base |
|---|---|
| Tutores (`DONOS`) | `/api/tutores` |
| Funcionários | `/api/funcionarios` |
| Pets | `/api/pets` |
| Consultas | `/api/consultas` |
| Medicamentos | `/api/medicamentos` |
| Vacinas | `/api/vacinas` |
| Consultas × medicamentos | `/api/consultamedicamentos` |

Todos expõem CRUD completo — `GET` paginado, `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}` — além de filtros específicos por recurso (e-mail, espécie, período, pendentes).

---

## Observabilidade

### Health checks

Implementados com `Microsoft.Extensions.Diagnostics.HealthChecks`, separados por tag.

A API não consome nenhum serviço externo — não há gateway de pagamento, fila, cache ou API de terceiros. A única dependência de infraestrutura é o banco Oracle, e é ela que o check de readiness verifica. Por isso não existe um terceiro health check registrado.

| Endpoint | Verifica | Respostas |
|---|---|---|
| `GET /health/live` | Liveness. Processo ASP.NET Core de pé. Não toca o banco. | `200 Healthy` |
| `GET /health/ready` | Readiness. Conectividade real com o banco via `AppDbContext`, timeout de 5s. | `200 Healthy` ou `503 Unhealthy` |
| `GET /health` | Agregado dos dois checks. | `200` ou `503` |

Liveness não depende do banco, para servir de sonda de reinício de container. Readiness valida a conexão: com o Oracle indisponível, `/health/ready` devolve 503 enquanto `/health/live` continua 200.

O `OracleHealthCheck` usa um `CancellationTokenSource` linkado com limite de 5 segundos, para que uma queda de rede não deixe a sonda pendurada. Falhas de conexão, timeout e exceções inesperadas viram `Unhealthy` com descrição fixa — a connection string e o stack trace não chegam à resposta.

Os dois cenários são cobertos por teste: `HealthCheckTests` exercita o banco disponível e `UnavailableDatabaseHealthCheckTests` sobe a aplicação apontando para um banco inacessível e confirma `503` em `/health/ready`, `200` em `/health/live` e a ausência de detalhes de conexão no corpo.

Resposta:

```json
{
  "status": "Healthy",
  "totalDurationMs": 12.4,
  "timestamp": "2026-08-26T13:20:11.4521Z",
  "correlationId": "69291123c0fa8fcb5fff9d8492aed136",
  "checks": [
    { "name": "self", "status": "Healthy", "durationMs": 0.03, "tags": ["live"] },
    { "name": "oracle", "status": "Healthy", "durationMs": 12.1, "tags": ["ready"] }
  ]
}
```

### Logging

Serilog configurado em `appsettings.json`, com saída simultânea para console e arquivo (`logs/clyvovet-.log`, rotação diária, retenção de 7 arquivos). Nível padrão Information, com override Warning para `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore` e `System`.

Correlação de requisições: o middleware aceita o header `X-Correlation-Id` do cliente (até 64 caracteres alfanuméricos) ou usa o TraceId do W3C Trace Context. O valor aparece em todas as linhas de log da requisição, no header da resposta, no campo `traceId` do ProblemDetails e como tag `clyvovet.correlation_id` no span.

E-mails são mascarados nos logs e nas mensagens de erro.

### Tracing e métricas

OpenTelemetry com instrumentação ASP.NET Core e runtime, mais um `ActivitySource` próprio usado em `ConsultaService`, `VacinaService`, `IntelligenceService` e nos repositórios de consulta, pet e vacina.

Os spans das camadas entram na mesma trace da requisição HTTP. Uma chamada a `GET /api/pets/{id}/inteligencia-preventiva` produz esta árvore, capturada com o Console exporter:

```
TraceId: bb32212f8de100f7aca8b30707065b94
  GET api/Pets/{id:int}/inteligencia-preventiva     span=3c6c70bc37dbfa48  parent=(raiz)
    IntelligenceService.GetIntelligencePreventiva    span=17e7067fd1f3dd6c  parent=3c6c70bc37dbfa48
      PetRepository.GetById                          span=8a4e7016dfbf1487  parent=17e7067fd1f3dd6c
```

`TracingTests` verifica essa cadeia automaticamente: registra um `ActivityListener`, faz a requisição HTTP e confirma que Controller, Service e Repository compartilham o mesmo `TraceId` e que o encadeamento pai/filho está correto.

Tempo de resposta vem de `http.server.request.duration`. Taxa de erros vem da métrica `clyvovet.http.errors`, dimensionada por método, status code e `error.kind` (client/server).

Em Development o Console exporter já vem habilitado por `appsettings.Development.json`: basta `dotnet run` e a primeira requisição imprime spans e métricas no console, sem definir nenhuma variável de ambiente. Em Production ele permanece desligado, e a exportação é escolhida pelas variáveis abaixo. `docker-compose.observability.yml` sobe o Aspire Dashboard em `http://localhost:18888`.

```bash
export OpenTelemetry__ConsoleExporter=true      # fora de Development; em Development já é o padrão
export OpenTelemetry__OtlpEndpoint=http://localhost:4317
```

---

## Como rodar os testes

```bash
dotnet test                                  # solution inteira — 176 testes
dotnet test ClyvoVetApi.Tests.Unit           # 131 unitários
dotnet test ClyvoVetApi.Tests.Integration    #  45 de integração
```

Os testes não tocam o Oracle e não exigem nenhuma variável de ambiente.

**Unitários.** xUnit com Moq nas dependências, padrão AAA e nomenclatura `MetodoTestado_Cenario_ResultadoEsperado`. Cobrem os oito Services (regras de negócio, validações, exceções de domínio e interações esperadas com os repositórios), o `AuthService` (credenciais válidas e inválidas, configuração ausente, expiração, issuer, audience e unicidade do jti), o `OracleHealthCheck` (banco inalcançável, falha inesperada, ausência de connection string na resposta e limite de tempo) e o `SensitiveDataMasker`. `TestDataFixture` gera entidades com valores padrão coerentes e é consumida via `IClassFixture` por `PetServiceTests` e `VacinaServiceTests`; como não guarda estado mutável, os testes seguem isolados.

**Integração.** `WebApplicationFactory<Program>` sobe a API real no ambiente `Testing`, com SQLite in-memory no lugar do Oracle — banco relacional de verdade, que aplica chaves estrangeiras e check constraints sem depender de Docker. `IntegrationTestFixture` concentra factory, conexão e seed; `IntegrationTestCollection` compartilha a fixture entre as classes via `ICollectionFixture` e garante execução sequencial. O banco é resetado a cada teste, o `AlertScheduler` não é registrado, e o logger do Serilog é substituído por um sem sinks para que a suíte não escreva em `logs/`.

Cobertura: autenticação (login válido e inválido, token ausente, malformado, expirado e assinado com outra chave), fluxos HTTP (201 com header `Location` confirmado por GET subsequente, 200, 204), erros (400 de validação, 400 de regra de negócio, 404, 401, formato do ProblemDetails), health checks (`/health/live`, `/health/ready` exercitando a conectividade com o banco, `/health` agregado, separação por tags, payload com durações e correlation ID, e o cenário de banco indisponível devolvendo 503) e tracing (propagação de `TraceId` entre Controller, Service e Repository, tag de correlação no span e ausência de dado pessoal nas tags).

### Cobertura de código

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Relatório por suíte em `<projeto>/TestResults/<guid>/coverage.cobertura.xml`. Última execução, somando as duas suítes:

| Camada | Linhas |
|---|---|
| Services | 92,6% |
| Models | 100% |
| DTOs | 100% |
| Middleware, Observability e Logging | 100% |
| Auth | 100% |
| Data | 93,7% |
| HealthChecks | 85,4% |
| Exceptions | 81,4% |
| Global | 59,8% (73,9% sem as migrations) |

O número global inclui Controllers e Repositories, exercitados apenas nos fluxos escolhidos para os testes de integração, além de `Program.cs` e das migrations.

---

## Limitações

- **Migrations dessincronizadas.** Os arquivos em `Migrations/` descrevem uma modelagem anterior e não refletem o `AppDbContext`. Precisam ser regeneradas junto com a disciplina de Database. O mesmo vale para `script.sql`, mantido por vínculo com outra disciplina — o DDL válido é o `script_bd.sql`.
- **Leitura pública.** Todos os `GET` seguem sem autenticação nesta Sprint, embora algumas respostas incluam nome, e-mail e telefone de tutores.
- **Rota de tutores.** `DonosController` responde em `/api/tutores`, herança da renomeação Tutor → Dono. `/api/donos` retorna 404.
- **AlertScheduler** não possui testes automatizados.
- **Docker não validado em execução.** `Dockerfile` e `docker-compose.yml` foram revisados estaticamente; a máquina de desenvolvimento não possui Docker instalado. O mesmo vale para o Aspire Dashboard — o caminho de observabilidade validado foi o Console exporter.
