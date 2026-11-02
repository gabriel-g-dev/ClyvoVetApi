# Banco de dados

O EF Core usa Oracle 21 para os dados relacionais. O modelo em
`src/ClyvoVetApi.Infrastructure/Data/AppDbContext.cs`, o snapshot e a migration
`20261025155040_OracleBaseline` representam sete tabelas: `DONOS`, `FUNCIONARIOS`,
`PETS`, `CONSULTAS`, `MEDICAMENTOS`, `CONSULTAS_MEDICAMENTOS` e `VACINAS`.

O modelo define seis FKs com exclusão em cascata, e-mails únicos de donos e
funcionários, peso `DECIMAL(5,2)`, valor de consulta `DECIMAL(10,2)` e datas
`TIMESTAMP(7)`. Os status aceitos são `A/C/R` para consultas e `P/A` para vacinas.
Os nomes das colunas usam aspas e letras minúsculas; essa distinção importa no
Oracle.

## Executar em um banco local novo

Requer .NET 9, Docker e Compose. Faça o restore e preencha `.env` conforme o
[README principal](../../README.md). Use uma sessão dedicada: variáveis já
definidas no terminal prevalecem sobre `.env` no Compose.

```powershell
dotnet tool restore
docker compose up -d --wait oracle-db mongo-db

# Use ORACLE_APP_USER e ORACLE_APP_PASSWORD do .env.
# A CLI no host usa localhost; a API no Compose usa oracle-db.
$oracleHostConnection = 'User Id=<usuario-local>;Password=<senha-local>;Data Source=localhost:1521/XEPDB1'
dotnet ef database update --connection $oracleHostConnection --project src/ClyvoVetApi.Infrastructure --startup-project src/ClyvoVetApi.Infrastructure

docker compose up -d --build api
```

A CLI .NET não carrega `.env` automaticamente. O argumento `--connection` vale
somente para o comando EF; não modifica a configuração da API. As senhas de
inicialização do Oracle XE 21 devem ter até 30 caracteres ASCII. Alterar essas
variáveis não recria usuários de um volume existente.

A API não aplica migrations ao iniciar. A baseline recusa schemas que já tenham
tabelas clínicas, inclusive `TUTORES`. Não use esse procedimento em banco
compartilhado ou preenchido. DDL Oracle pode permanecer confirmado após uma
falha; investigue o estado do schema antes de tentar novamente. O health check
de readiness verifica conectividade, não a existência das tabelas.

## Banco existente e alinhamento com André

Antes de usar o banco do grupo, André precisa conferir o schema e autorizar uma
estratégia de atualização:

1. Fazer backup e identificar a versão Oracle, o schema e o histórico
   `__EFMigrationsHistory`.
2. Executar [inspect-existing-oracle.sql](inspect-existing-oracle.sql), que
   consulta apenas metadados. Comparar as sete tabelas e 36 colunas com
   [oracle-schema.sql](oracle-schema.sql), incluindo nomes, tipos, nulabilidade,
   identidades/IDs existentes, PKs, FKs, índices únicos e checks.
3. Se o schema corresponder integralmente ao modelo atual, avaliar a adoção da
   baseline no histórico, preservando os registros anteriores. Essa decisão não
   autoriza executar o DDL de criação novamente.
4. Se houver diferenças, preparar uma migration incremental e validá-la em uma
   cópia do banco. O schema antigo com `TUTORES` não equivale ao atual com `DONOS`.

Não execute `database update`, os scripts de criação ou o rollback `Down` da
baseline no banco compartilhado sem esse alinhamento. O rollback remove tabelas.

Confirme também as regras de exclusão: apagar um dono remove seus pets, consultas,
vacinas e prescrições relacionadas; apagar funcionários ou medicamentos também
remove os relacionamentos correspondentes. As evoluções no MongoDB permanecem
após a exclusão relacional, portanto a retenção do histórico precisa ser acordada.

## Scripts e migrations

| Arquivo | Uso |
| --- | --- |
| [oracle-migrations.sql](oracle-migrations.sql) | Baseline e histórico EF para schema novo; não é um script idempotente. |
| [oracle-schema.sql](oracle-schema.sql) | DDL atual para comparação ou criação manual de schema novo, sem histórico EF. Não aplicar junto com a baseline. |
| [inspect-existing-oracle.sql](inspect-existing-oracle.sql) | Inspeção somente leitura de schema, constraints, índices, identidades e histórico EF. |
| [script_bd.sql](../../script_bd.sql) | DDL mantido para a disciplina Database; corresponde ao modelo de sete tabelas, sem histórico EF. |
| [script.sql](../../script.sql) e [migrations legadas](../migrations-legadas/) | Modelagem anterior com `TUTORES` e expansões antigas. Preservadas para revisão com André; não combinar com a baseline atual. |

Os dois checks de status de `script_bd.sql` usam os nomes de coluna entre aspas e
literais duplicados dentro de `EXECUTE IMMEDIATE`. Esses ajustes corrigem a sintaxe
do SQL dinâmico. A aplicação desse arquivo ainda exige validação em Oracle local.

Para exportar novamente os SQLs atuais e conferir alterações no modelo, sem
conectar ao banco:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Export-OracleSql.ps1
dotnet ef migrations has-pending-model-changes --project src/ClyvoVetApi.Infrastructure --startup-project src/ClyvoVetApi.Infrastructure
```

O exportador ajusta o escape dos checks gerados pelo provider Oracle, os
terminadores SQL*Plus e a codificação UTF-8 sem BOM.

## MongoDB

As evoluções clínicas são documentos na coleção `evolucoes_clinicas`, no banco
`clyvovet` por padrão. Cada documento contém ID, ID do pet, texto, autor, data UTC e
tags. O repositório cria o índice composto `PetId/RegistradoEmUtc/_id` ao inserir
ou listar documentos. Essa criação requer permissão de índice no MongoDB.

Configure `Mongo__ConnectionString`, `Mongo__DatabaseName` e, se necessário,
`Mongo__CollectionName`. A API no Compose usa `mongodb://mongo-db:27017`; fora do
container, use o endereço local correspondente. Oracle e Mongo são publicados
em loopback pelo Compose. Para o ambiente do grupo, alinhe com André os endereços,
credenciais, permissões e política de retenção; o Mongo local não exige autenticação.

## Testes com bancos descartáveis

Os testes HTTP locais usam SQLite e um repositório documental em memória; eles
não substituem a validação nos providers Oracle e MongoDB. Os testes reais têm
opt-in explícito e devem apontar somente para bancos locais descartáveis.

```powershell
$env:ORACLE_TEST_PASSWORD = '<senha-ASCII-forte-com-ate-30-caracteres>'
docker compose -f docker-compose.tests.yml -p clyvovet-tests up -d --wait
$env:CLYVOVET_ORACLE_TEST_CONNECTION = "User Id=clyvovet_test;Password=$env:ORACLE_TEST_PASSWORD;Data Source=localhost:11521/XEPDB1"
$env:CLYVOVET_ORACLE_ALLOW_MIGRATIONS = 'true'
$env:CLYVOVET_MONGO_TEST_CONNECTION = 'mongodb://localhost:27029'
dotnet test ClyvoVetApi.Tests.Integration --filter 'Category=OracleReal|Category=MongoReal' --results-directory artifacts/TestResults/providers
```

O teste Oracle aplica a baseline, confirma persistência por nova conexão e limpa
somente o grafo que criou. Os testes Mongo criam bancos `clyvovet_tests_<guid>` e
os excluem ao terminar. Sem as variáveis correspondentes, esses casos são
ignorados pelo xUnit. Não use conexões de ambientes compartilhados nesses testes.
