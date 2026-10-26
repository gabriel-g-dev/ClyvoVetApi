# Oracle e migrations da Sprint 4

O contexto EF Core está em `src/ClyvoVetApi.Infrastructure/Data`. A migration ativa
`20261025155040_OracleBaseline` descreve as sete tabelas atuais, com nomes, tipos,
PKs, seis FKs com cascade, índices únicos de e-mail e checks de status compatíveis
com a modelagem da disciplina Database. O snapshot corresponde ao contexto.
A compatibilidade SQL está fixada em Oracle 21, inclusive no contexto de design
time, para corresponder à imagem Oracle XE 21 do Compose.

As migrations da modelagem anterior (TUTORES, quatro tabelas e colunas antigas)
estão preservadas como texto em `docs/migrations-legadas`, fora da compilação.
Não foram executadas, apagadas do banco ou convertidas automaticamente.

## Banco novo e descartável

Requer .NET 9 e Docker com Compose. O arquivo de testes não usa volumes
persistentes e possui portas próprias; não aponta para o banco compartilhado.

```powershell
$env:ORACLE_TEST_PASSWORD = '<senha-ASCII-forte-com-ate-30-caracteres>'
docker compose -f docker-compose.tests.yml -p clyvovet-sprint4 up -d --wait
$env:CLYVOVET_ORACLE_TEST_CONNECTION = "User Id=clyvovet_test;Password=$env:ORACLE_TEST_PASSWORD;Data Source=localhost:11521/XEPDB1"
$env:CLYVOVET_ORACLE_ALLOW_MIGRATIONS = 'true'
$oracleResults = Join-Path (Get-Location) ('artifacts/TestResults/oracle-' + [guid]::NewGuid().ToString('N'))
dotnet test ClyvoVetApi.Tests.Integration --filter Category=OracleReal --results-directory $oracleResults --logger trx --logger 'console;verbosity=normal'
```

Substitua o placeholder por uma senha local. Oracle XE 21 aceita até 30 caracteres
ASCII nesses campos; por exemplo, 12 bytes aleatórios em hexadecimal, prefixados
por `A9`, produzem 26 caracteres. Isso é independente da chave JWT, que exige
pelo menos 32 bytes. Não reutilize os segredos de outro ambiente.

O teste aplica `Database.MigrateAsync`, confirma um grafo com três pets e três
consultas e abre um novo contexto/conexão para aplicar migrations novamente.
Confere histórico, ausência de pendências e preservação do grafo, além de busca,
paginação, empate por ID e ordenação decimal por peso/valor no provider Oracle.
Uma tentativa de status inválido verifica o check e é revertida por rollback.

O grafo válido é confirmado para ser visível à nova conexão. Ao terminar, a limpeza
remove somente as três raízes criadas pelo teste, conferidas por ID e marcador
aleatório; os cascades removem seu próprio grafo. Não há `EnsureDeleted`, `DROP`
ou reset do schema. As identidades podem avançar mesmo após a limpeza. Nunca
informe um banco compartilhado. Sem as duas variáveis de opt-in, o teste fica
explicitamente **ignorado**, sem simular uma execução Oracle.

Para aplicar manualmente no schema novo do Compose principal, siga a sequência
de clone/restore do [README](../../README.md#instalação-e-execução), preencha `.env`
e use o usuário/senha definidos em `ORACLE_APP_USER`/`ORACLE_APP_PASSWORD`:

Use uma sessão dedicada e confira se ela já define
`ConnectionStrings__OracleConnection`: variáveis da sessão prevalecem sobre `.env`
na interpolação do Compose. Não continue com uma variável apontando para outro
ambiente; ajuste a sessão conscientemente antes de subir os serviços.

```powershell
dotnet tool restore
docker compose up -d --wait oracle-db mongo-db
# A CLI .NET no host não lê .env; localhost acessa a porta publicada pelo Compose.
$oracleHostConnection = 'User Id=<usuario-local>;Password=<senha-local>;Data Source=localhost:1521/XEPDB1'
dotnet ef database update --connection $oracleHostConnection --project src/ClyvoVetApi.Infrastructure --startup-project src/ClyvoVetApi.Infrastructure
docker compose up -d --build api
```

A API no container usa `oracle-db:1521/XEPDB1`; esse nome de serviço não é o
endereço da CLI executada no host. Em outro banco novo autorizado, substitua a
conexão pelo endereço correspondente. Não use esse fluxo em schema compartilhado
ou já preenchido. Trocar variáveis de inicialização do Compose não recria nem
altera usuários de um volume Oracle existente.
O argumento `--connection` aplica o destino somente ao comando EF; a variável
PowerShell local não altera a conexão interpolada pelo Compose.

A API não aplica migrations na inicialização; só inicie seu uso após sucesso do
comando EF. Readiness comprova conexão, não existência do schema.
A primeira operação da baseline
recusa schemas que já contenham qualquer tabela clínica (inclusive TUTORES).
Se uma criação falhar, o Oracle pode manter DDL já confirmado: inspecione o schema
e resolva a causa; não repita comandos destrutivos para tentar corrigir.

## Banco existente

Não execute `oracle-schema.sql`, `oracle-migrations.sql` ou `database update` sobre
um banco existente para tentar alinhá-lo. A sequência segura depende do schema real:

1. O responsável por Database realiza backup e registra schema, versão Oracle e
   histórico `__EFMigrationsHistory` existentes.
2. Execute `inspect-existing-oracle.sql` (somente leitura). Compare colunas,
   capitalização, identidade, precisão, nulabilidade, PKs, FKs, índices e checks
   com `oracle-schema.sql`. São sete tabelas e 36 colunas de negócio.
   O diagnóstico inclui versão, colunas/ordem/expressões e DDL dos índices
   standalone, opções/metadados de identidade e histórico EF quando presente.
   `LAST_NUMBER` de sequência é um limite de cache, não uma garantia do próximo ID;
   a inspeção não consome `NEXTVAL` nem altera a sequência.
3. Se o schema for exatamente o modelo atual, adote a baseline apenas no histórico,
   após revisão do responsável. Se a tabela de histórico não existir, use somente
   seu bloco de criação no início de `oracle-migrations.sql`; em seguida registre
   apenas a linha `20261025155040_OracleBaseline` com `ProductVersion` `9.0.4`.
   Preserve registros antigos e dados de negócio. A criação do histórico e seu
   registro são alterações deliberadas de banco e não foram executadas nesta entrega.
4. Se houver diferenças, não marque a baseline como aplicada: prepare migração
   incremental específica com o responsável e valide uma cópia do banco primeiro.
   Um schema antigo com TUTORES não é equivalente ao atual com DONOS.
5. Após uma adoção aprovada, confira `GetAppliedMigrations`/`GetPendingMigrations`
   ou `dotnet ef migrations list`. Mantenha o backup e a evidência da comparação.

O método `Down` da baseline é o rollback convencional gerado pelo EF e remove
tabelas. Não deve ser executado em banco compartilhado ou com dados importantes.
Nenhum rollback de migration foi executado nesta tarefa.

No Compose principal, Oracle e Mongo são publicados apenas em `127.0.0.1:1521`
e `127.0.0.1:27017`; Dashboard/OTLP também usam apenas `127.0.0.1:18888` e
`127.0.0.1:4317`. A API continua publicada na porta 8080. Os nomes de serviço
e a rede entre containers permanecem iguais. Acesso remoto aos bancos ou ao
Dashboard exige configuração explícita com André, incluindo autenticação e
controle de acesso; o Dashboard local permite acesso anônimo.

Antes de adotar o banco do grupo, alinhe com André os nomes exatos e as aspas das
colunas, `DECIMAL(5,2)` para peso, `DECIMAL(10,2)` para valor, datas `TIMESTAMP(7)`,
identidades compatíveis com os IDs existentes, índices únicos de e-mail e checks
de status. Revise também os seis cascades: excluir tutor pode apagar pets,
consultas e vacinas; excluir funcionário pode apagar consultas e prescrições.
As evoluções no MongoDB permanecem após a exclusão relacional. Esse comportamento
exige uma política de retenção acordada; nenhuma adoção ou alteração foi feita
no banco compartilhado.

## SQL exportado e compatibilidade

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Export-OracleSql.ps1
dotnet ef migrations has-pending-model-changes --project src/ClyvoVetApi.Infrastructure --startup-project src/ClyvoVetApi.Infrastructure
```

- `oracle-migrations.sql`: criação do histórico e aplicação da baseline; inclui
  interrupção de SQL*Plus em caso de erro SQL. A execução exige schema novo;
  o arquivo completo não deve ser reaplicado como script idempotente.
- `oracle-schema.sql`: DDL do modelo, para comparação e criação manual de schema
  novo. Não registra migrations; não aplique ambos no mesmo banco.
- `inspect-existing-oracle.sql`: diagnóstico somente leitura para um banco existente.

Foi identificado um problema verificável no `script_bd.sql` original: as colunas
`"consulta_status"` e `"vacina_status"` são criadas com aspas e minúsculas, mas os
checks usam nomes sem aspas; além disso, os literais dos checks aparecem sem
duplicação de aspas dentro de `EXECUTE IMMEDIATE`. O arquivo original foi
preservado por pertencer à integração com Database/DevOps. O novo DDL corrige os
dois pontos, mantendo a estrutura relacional. A revisão deve ser compartilhada
com o responsável pela disciplina antes da próxima aplicação do script original.

O provider Oracle EF 9.23.60 também produz o segundo problema ao exportar checks
no `CREATE TABLE` dinâmico. Por isso a migration adiciona os checks com `ALTER TABLE`
direto após criar as tabelas, e o exportador normaliza os literais do DDL e o
separador `/` para SQL*Plus. Os arquivos SQL são gravados em UTF-8 **sem BOM**,
evitando um prefixo que alguns clientes rejeitam antes do primeiro `WHENEVER`.
O próprio `.ps1` mantém BOM para reconhecer corretamente os textos no PowerShell 5.1.
Os testes de modelo verificam a geração; a aplicação Oracle descrita abaixo foi
executada por `Database.MigrateAsync`.

`script.sql` também foi preservado: representa a modelagem legada com `TUTORES`,
colunas `Id` e tabelas anteriores. Não o combine com `script_bd.sql` nem com os
novos arquivos de baseline. A correção dos arquivos originais deve ser coordenada
com Database/DevOps antes de qualquer aplicação.

## Evidência e limites desta máquina

O [resumo versionável de validação](../validation/sprint4-results.json) consolida
os resultados sanitizados. Os testes e comandos deste guia permitem reproduzi-los;
os caminhos em `artifacts` abaixo são evidências brutas locais complementares,
não arquivos necessários para executar o projeto em outro computador.

Em 25/10/2026, a revisão adversarial encontrou Ubuntu WSL 2 por inspeção fora
da restrição inicial do sandbox. Um Docker Engine oficial temporário e isolado
permitiu iniciar Oracle XE 21 em container/schema novos, com credenciais geradas
para a auditoria. Nenhum serviço Docker foi instalado e nenhum banco compartilhado
foi acessado.

`OracleReal` passou com a baseline `20261025155040_OracleBaseline`: primeira e
segunda aplicação, nova conexão, grafo clínico, busca/páginas, peso/valor ordenados
e check real. Separadamente, um marcador confirmado e o histórico EF sobreviveram
ao restart real do container; o marcador foi removido por identificação exata.
Os TRX estão em `artifacts/audit2/oracle-real`, e a prova de restart em
`artifacts/audit2/oracle-restart-verified.txt`. Essa execução comprova a criação
do zero no Oracle 21; não aprova automaticamente um schema existente.

A inspeção SQL real, executada como usuário da aplicação, confirmou sete tabelas,
36 colunas, sete PKs, seis FKs com cascade, dois índices únicos de e-mail e sete
identidades. O resultado está em `artifacts/audit2/oracle-schema-inspection.txt`.
Um processo nativo da API também completou 90 requisições contra Oracle/Mongo
reais, cobrindo os sete CRUDs, leitura após escrita, links, precisão decimal,
filtros/páginas, autorização e histórico documental. O relatório sanitizado está
em `artifacts/audit2/native-http-report.json`. As suítes Debug e Release passaram,
cada uma, com 438 casos aprovados e nenhum caso ignorado.

Geração/exportação do SQL e consistência do snapshot também foram verificadas.
Os três arranjos Compose passaram por `config --quiet` com Compose oficial 5.6.0
e checksum conferido. O build real da imagem da API passou sem avisos ou erros.
No runtime e após restart da API, live/ready/OpenAPI retornaram 200, o GET clínico
anônimo retornou 401, o processo usou UID 1654 (`app`) e os logs JSON/arquivo
funcionaram. Nesse smoke o Oracle do Compose ficou sem migrations; readiness
comprova conexão, não a existência do schema clínico.

O daemon descartável tem forwarding/iptables desabilitados. A tentativa na rede
bridge original teve readiness 503; um override temporário restrito compartilhou
o namespace privado somente entre os três containers novos, com portas em loopback,
e permitiu o smoke saudável. A alternativa de host networking foi rejeitada pelo
review automático antes de executar e substituída por esse caminho privado.
Não houve alteração de firewall/sysctl. Depois do smoke, os bindings versionados
de Oracle/Dashboard foram restringidos a loopback e os três arranjos passaram
novamente por `config --quiet`, sem recriação de containers. Isso não altera
a rede entre os serviços nem a publicação da API 8080.
A rede bridge padrão ainda precisa ser exercitada em um daemon com rede habilitada.

Os containers e volumes do projeto de smoke foram removidos após conferir seus
IDs, nomes e labels; o Oracle usado nas provas de persistência foi preservado
durante essa limpeza. Evidências sanitizadas estão em
`artifacts/audit2/docker-runtime/validation-summary.json`. Arquivos de resultados
e segredos temporários não são versionados. Na integração com o grupo, confirme
também nomes DNS/serviços, portas e autenticação MongoDB fora da rede local.

No fechamento da auditoria em 26/10/2026, o ambiente Oracle/Docker restante e o
Mongo temporário também foram removidos, junto com seus dados/credenciais próprios.
O Ubuntu WSL existente foi preservado. As confirmações estão em
`artifacts/audit2/docker-cleanup.json` e `mongo-cleanup.json`; as evidências das
execuções e reinicializações continuam disponíveis nos artefatos locais.

A estratégia de baseline segue a orientação oficial de [gerenciamento de migrations
do EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing):
um banco existente exige inspeção, backup e registro da baseline sem repetir seu DDL.
