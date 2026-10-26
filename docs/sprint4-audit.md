# Auditoria da Sprint 4 — ClyvoVet API

Execuções: 25 e 26/10/2026. Auditoria e última revisão técnica, com revisão independente de código, contratos, entrega e instalação. Escopo: backend .NET local e ambientes descartáveis. **Sem commit, push, deploy, alteração do banco compartilhado ou contratação de serviços.** Não foi atribuída nota automática.

## Conclusão para revisão

A lacuna principal da primeira entrega foi resolvida: a migration foi aplicada em **Oracle XE 21 real**, em schema novo e isolado, e a segunda aplicação preservou os dados. Oracle e MongoDB também preservaram registros após reinicialização real. Debug e Release passaram, cada um, com **438 testes aprovados, zero falhas e zero ignorados**. Uma API nativa completou **90 requisições HTTP contra Oracle e MongoDB reais**.

A revisão encontrou e corrigiu falhas verificáveis de precisão decimal, navegação HATEOAS com parâmetros prefixados, schemas OpenAPI compartilhados, privacidade de logs/spans e dependências com advisories conhecidos. O build e o runtime da imagem Docker foram executados, incluindo restart da API.

**Ainda há uma validação externa de DevOps:** a rede bridge padrão do Compose não foi comprovada no daemon temporário, que opera sem forwarding/iptables. O runtime saudável usou um override privado entre os três containers novos. A adoção da baseline em um schema existente também depende de inspeção e decisão de André. A auditoria local está entregue; não há declaração de aprovação integral da infraestrutura do grupo ou de 100/100.

## Última revisão e instalação do zero — 26/10/2026

A última passagem examinou os itens transcritos da rubrica, os projetos, as referências, os contratos anteriores e o conteúdo da futura entrega. **Os dois slides originais não estão disponíveis entre os anexos/workspace**: foi conferida integralmente a transcrição de requisitos e penalidades do pedido inicial, sem alegar inspeção literal das imagens.

Foi exportada uma cópia limpa dos **182 arquivos entregáveis então existentes**, incluindo os novos ainda não rastreados, sem `.git`, `.env`, `bin`, `obj`, logs ou artefatos anteriores. Restore com cache NuGet novo e `--no-cache`, restauração da ferramenta EF, build Debug/Release e publish passaram. Como não há autorização de commit, essa prova usa os arquivos da futura entrega, não um clone remoto da Sprint 4 ainda não publicada. As instruções de clone estão no README.

Em dois containers novos, Oracle XE **21.3.0.0.0** e MongoDB **8.0.34**, a CLI aplicou a baseline do zero usando `--connection`; uma segunda aplicação informou banco atualizado, e não há mudanças pendentes no modelo. Na cópia limpa, **197 unitários + 241 integrações passaram em Debug e novamente em Release: 438 aprovados, zero falhas e zero ignorados em cada execução**. Incluem um `OracleReal`, sete `MongoReal`, regressões anteriores, contratos REST/JWT/HATEOAS, health, logging e tracing. Nenhum teste foi removido ou teve assertion reduzida nesta rodada. A cobertura de linhas permaneceu em 100% Domain e 99,65% Application.

O processo da API dessa cópia limpa executou novamente as **90 requisições HTTP**, com **333 verificações aprovadas**, contra os dois bancos reais. Esse roteiro usa ambiente ASP.NET `Testing`, sem scheduler; os dados e os providers são reais. A preparação do schema precedeu o uso clínico. A consulta NuGet de dependências diretas/transitivas retornou zero advisories conhecidos. Os três arranjos Compose foram revalidados; os bancos usaram somente namespace privado entre containers, sem host networking ou alteração de firewall/sysctl. A rede bridge padrão continua pendente.

Foram corrigidos dois problemas de reprodução no README/guia: a sequência de clone → dependências → bancos → migration → API → login/OpenAPI/testes ficou explícita; e a conexão da migration no host usa variável local com `--connection`, para não sobrescrever por ambiente a conexão `oracle-db` usada pelo Compose. A precedência de variáveis da sessão sobre `.env` está explicada. O comando de cobertura também explicita a política de execução somente do processo PowerShell; os dez blocos PowerShell do README/guia passaram no parser.

**Arquivos removidos nesta rodada: nenhum.** As referências conferidas mostraram uso por DI, MVC, EF design time, atributos, serialização ou configuração. Os overloads antigos sustentam contratos/testes anteriores; exporters, sinks e o pin PKCS têm função. Não se retirou código carregado indiretamente nem se fez refatoração cosmética. As exclusões antigas mostradas pelo Git pertencem aos movimentos de camadas/arquivamento de migrations da implementação, não a uma nova retirada de funcionalidades.

O [resumo versionável das execuções](validation/sprint4-results.json) registra contagens, casos reais, hashes dos relatórios de entrada, ambiente e limites. O [inventário da futura entrega](validation/git-delivery.json) enumera **127 arquivos novos, 32 rastreados modificados e 68 origens removidas pelos movimentos anteriores**. Foram conferidos 184 arquivos físicos essenciais, nenhum escondido pelo `.gitignore`. Esses documentos, os testes e os comandos de reprodução acompanham o repositório; os TRX/XML/logs locais em `artifacts/audit3` são complemento. Nenhum fonte C#, projeto ou solução mudou depois dessa validação.

Os dois containers/volumes exclusivos, o daemon Docker temporário e suas credenciais foram removidos após conferir propriedade, caminhos e processos. O Ubuntu WSL e recursos externos foram preservados. A confirmação sanitizada da limpeza está incorporada ao resumo versionável.

## Base de comparação e método

Branch `main`, HEAD `83b6dd76b55e471d5f5344f60a80f3617f3f9928`. A segunda auditoria começou sobre a implementação ainda não commitada da Sprint 4. O inventário SHA-256 inicial está em `artifacts/audit2/source-before.json`; o inventário dos fontes usados nos testes finais está em `artifacts/audit2/source-for-final-tests.json`.

Os relatórios da primeira entrega confirmavam **184 unitários + 172 integrações aprovados e um Oracle ignorado**: 357 casos descobertos, 356 aprovados. Esses números não foram reapresentados como novas execuções. Na implementação inicial, antes da Sprint 4, existiam 131 unitários e 45 integrações; as suítes anteriores foram preservadas e adaptadas aos contratos de segurança explícitos.

A revisão confrontou código, referências de projetos, TRX, XML Cobertura, SQL, OpenAPI servido e respostas HTTP. Testes adicionais reproduziram falhas antes das correções; as reproduções estão em `artifacts/audit2/reproductions`. Uma passagem adversarial independente examinou autorização, contratos, migrations, consultas reais, privacidade e limitações dos testes. Não foram relaxadas assertions para ocultar defeitos.

As provas detalhadas de 25/10 permanecem em `artifacts/audit2`; as novas coletas estão em `artifacts/audit3`. Esses diretórios são locais e ignorados pelo Git. A entrega inclui o resumo sanitizado em `docs/validation`, sem depender desses diretórios para instalar, compilar ou repetir os testes. Configuração privada e credenciais descartáveis não integram a entrega.

## Matriz da rubrica

Os pesos abaixo são os da rubrica, não notas atribuídas. “Validado localmente” descreve a evidência executada e seu alcance.

| Bloco / peso | Item conferido individualmente | Evidência e alcance |
|---|---|---|
| Arquitetura — 30 | Clean Architecture | Quatro projetos, dependências voltadas aos contratos internos, sem ciclos; teste de fronteira impede ASP.NET/EF/Oracle/Mongo em Domain/Application. |
| Arquitetura — 30 | SOLID | Services por responsabilidade, interfaces de repositório, composição por DI e invariantes de domínio; revisão não encontrou abstração extra a remover com segurança. |
| Arquitetura — 30 | Clean Code | Regras preventivas puras e código existente preservado; revisão de referências/comentários/configuração sem limpeza artificial. É avaliação de código, não nota derivada de cobertura. |
| Arquitetura — 30 | Dependency Injection | Registros concretos em `Program.cs`, resolução e execução dos fluxos pelas integrações e API real. |
| Arquitetura — 30 | Exceções globais | `GlobalExceptionHandler`, ProblemDetails com correlação e cenários 400/404/500/503; 401/403 coerentes. |
| REST — 20 | Swagger/OpenAPI | Documento servido com 34 paths, Bearer, schemas/defaults e referências locais válidas; Scalar disponível. |
| REST — 20 | Paginação | Sete coleções e evoluções com limites, contagem anterior à página, casos vazios/limites e navegação testados. |
| REST — 20 | Ordenação | Campos permitidos e desempate por ID; testes de ordem textual, datas e decimal no provider Oracle real. |
| REST — 20 | Filtros | Busca e consultas especializadas anteriores preservadas; filtros tag/período Mongo e validações de query exercitados. |
| REST — 20 | HATEOAS nas consultas | Links de recurso/página/relação e cabeçalho `Link`; testes seguem `self`/`next`, inclusive query prefixada. Arrays antigos permanecem arrays. |
| REST — 20 | JWT/autorização | Oito controllers clínicos protegidos por policy, login e 401/403 testados. Conta clínica única; não há isolamento por tutor. |
| Persistência — 20 | EF Core e migration aplicada | Baseline criada do zero no Oracle 21 pela CLI e testada por EF, segunda aplicação sem mudanças, grafo/constraints reais. Restart comprovado em 25/10; schema existente exige inspeção. |
| Persistência — 20 | MongoDB funcional | Sete testes reais, escrita/leitura/índice/novo contexto e fluxo HTTP com Oracle; restart comprovado em 25/10. Contrato append-only, sem update/delete documental. |
| Persistência — 20 | Repository Pattern | Interfaces Application e implementações EF/Mongo Infrastructure usadas por Services; sem acesso de controller diretamente aos bancos. |
| Monitoramento/testes — 20 | Health Checks | Live/ready/agregado, 200 com bancos reais e 503 em falha; health confirma conexão, não existência das tabelas clínicas. |
| Monitoramento/testes — 20 | Logging estruturado | Serilog JSON/arquivo, correlação, formatter configurado e canários de privacidade testados; tracing/métricas anteriores preservados. |
| Monitoramento/testes — 20 | xUnit unitário/integração AAA | 197 + 241 casos aprovados em ambas as configurações; Arrange/Act/Assert com leitura após escrita, links, falhas e assertions de dados. Substitutos e casos reais são identificados. |
| Monitoramento/testes — 20 | Cobertura Domain/Application | 150/150 linhas e 854/857: 100% / 99,65%. União por linha de XML únicos; sem exclusões novas ou soma de percentuais de branches. |
| Documentação — 10 | README completo/integrantes | Objetivo, stack, membros/RMs, ambiente, exemplos, contratos e limitações conferidos. |
| Documentação — 10 | Diagrama fiel | Referências API → Application/Infrastructure, Application → Domain, Infrastructure → Application e persistência Oracle/Mongo coerentes com os projetos. |
| Documentação — 10 | Endpoints documentados | Tabelas/exemplos no README e OpenAPI servido; alterações de segurança, precisão e rotas anteriores explícitas. |
| Documentação — 10 | Instalação/execução/testes | Fluxo do zero explícito com migration antes da API; cópia limpa e cache novo validados. Resumo sanitizado acompanha o Git; bridge padrão ainda exige André. |

As penalidades transcritas também foram verificadas: build aprovado (sem evidência de projeto sem compilar, -20), README presente/revisado (sem evidência de ausência, -10) e suítes executadas (não há ausência total de testes, -30). Isso não atribui nota nem substitui a conferência dos slides originais quando disponíveis.

## Arquitetura e contratos preservados

Domain contém entidades, exceções, invariantes da evolução clínica e regra preventiva com data de referência explícita. Application contém casos de uso, DTOs, validação BCL e interfaces de persistência. Infrastructure contém EF/Oracle, Mongo, migrations e health checks de banco. A API mantém HTTP, JWT, HATEOAS, ProblemDetails, observabilidade, scheduler e composição da DI. As referências foram verificadas sem ciclos; os fluxos executados resolvem as dependências concretas.

Os DTOs anteriores e nomes de domínio foram mantidos onde possível. `/api/tutores` continua sendo a rota de `DONOS`; não foi inventado alias `/api/donos`. O request de pet mantém `tutorId`; as respostas mantêm `donoId`/`donoNome` ou `tutorId` conforme o DTO anterior. Os envelopes e campos anteriores continuam presentes, com `links` aditivo. As sete famílias relacionais mantêm GET/lista, GET/id, POST, PUT e DELETE; as consultas adicionais estão inventariadas no README e nos testes de contrato.

A comparação com HEAD confirmou **53 actions HTTP anteriores com os mesmos métodos e templates de rota**: login e 52 actions clínicas; três actions de evolução foram acrescentadas. Os testes anteriores de regras/CRUD, inteligência preventiva, autenticação, health, métricas e tracing permanecem na solução e na execução completa. A proteção dos GETs é a alteração deliberada de contrato descrita abaixo, não a remoção desses endpoints.

As mudanças deliberadas são: autenticação em todas as operações clínicas; `scope=clinical`; IDs de associação positivos; datas relacionais obrigatórias a partir de 1900; páginas e filtros limitados; períodos completos/coerentes; e duas casas decimais significativas para peso/valor. Tokens antigos precisam de novo login. OpenAPI, Scalar e health continuam públicos. Novos endpoints documentais ficam em `/api/pets/{petId}/evolucoes` e no GET individual correspondente.

## Defeitos encontrados e corrigidos

| Defeito comprovado | Correção e verificação |
|---|---|
| Peso/valor com três casas eram aceitos, mas Oracle armazena duas, permitindo divergência entre resposta e leitura posterior. | `TwoDecimalPlacesAttribute` nos dois requests. `12.3400` permanece válido; `12.345` recebe 400 antes de mutação. Doze casos unitários e oito HTTP verificam criação/alteração, ausência de mutação e persistência exata. A ordenação decimal também foi exercitada no Oracle real. |
| Em evoluções, uma query como `query.page` gerava `next` com `page` plano e preservava o parâmetro antigo, repetindo a primeira página. | O filtro HATEOAS identifica o prefixo de model binding usado e escreve os parâmetros de navegação no mesmo formato. Testes seguem os links com variantes prefixadas; as sete listas relacionais continuam usando parâmetros planos. |
| O OpenAPI compartilhava schema primitivo e propagava regex `asc/desc` e limites indevidos para outros parâmetros string. | Transformer por operação reconstrói os schemas de parâmetros com metadados de validação/defaults e declara ProblemDetails. Testes reproduziram a contaminação e agora verificam seu isolamento. A atualização do pacote sozinha não resolvia o defeito. Um acesso a `Parameters` nulo introduzido durante a correção foi detectado na suíte e corrigido antes da validação final. |
| Caminhos contendo e-mail apareciam em spans/logs do framework; o login registrava usuário submetido; mensagens arbitrárias de exceção podiam expor conteúdo sensível. | `SafeJsonFormatter`, mascaramento ampliado, login genérico e `SafeTelemetryProcessor` antes dos exporters. O processador usa template de rota e remove URL/query e dados de exceção sensíveis. Quatro testes com canários falhavam antes e passaram após a correção, incluindo logs do framework, eventos/tags/status dos spans e exceções. |
| O coletor global de spans podia atribuir ao teste de inteligência um span de outro teste Mongo em paralelo. | A busca é restrita ao `TraceId` da própria requisição; assertions de pai/filho e propagação foram mantidas. Debug e Release completos passaram após a correção. |
| Exportação SQL via PowerShell 5 podia inserir BOM antes de `WHENEVER`, prejudicando SQL*Plus. | SQL exportado com UTF-8 sem BOM, separadores/literais normalizados e interrupção em erro. O `.ps1` mantém BOM para seus textos UTF-8 no PowerShell 5.1. Ambos os SQL exportados foram conferidos. |
| Auditoria NuGet encontrou advisories em dependências transitivas SharpCompress, Snappier, PKCS e SQLite nativo. | MongoDB.Driver 3.12.0, JWT/OpenAPI/MVC Testing 9.0.21, PKCS 9.0.21 e bundle SQLite de testes 3.0.5. Restore, builds, testes e publish passaram. A consulta final de dependências diretas/transitivas retornou zero advisories conhecidos na fonte consultada. |

Foram acrescentados 53 casos REST abrangendo CRUD anterior, campos permitidos de ordem/busca, prefixos HATEOAS e OpenAPI. Os testes de privacidade e os casos reais de persistência complementam a revisão; não substituem a análise de código. O script de cobertura exige agora diretório de resultados explícito para evitar mistura acidental de coletas antigas. Os bindings versionados de Oracle, Dashboard e OTLP foram limitados a loopback; Mongo já usava esse limite, e a publicação da API na porta 8080 foi preservada.

## Validação final executada

| Verificação | Resultado |
|---|---|
| Restore da solução | Sucesso nos seis projetos; SDK 9.0.317. |
| Build Debug / Release | Ambos com zero avisos e zero erros. |
| Publish Release | Sucesso na cópia limpa em `artifacts/audit3/publish`; saída anterior de 25/10 em `artifacts/audit2/publish`. |
| Testes Debug | **197 unitários + 241 integrações = 438 aprovados; 0 falhas; 0 ignorados.** |
| Testes Release | **197 unitários + 241 integrações = 438 aprovados; 0 falhas; 0 ignorados.** |
| Testes reais incluídos | Sete `MongoReal` e um `OracleReal`, aprovados nas duas configurações. |
| HTTP nativo com Oracle/Mongo reais | 90 requisições, todos os status/assertions esperados. |
| Modelo EF | `has-pending-model-changes`: nenhuma alteração; CLI confirmou banco atualizado na reaplicação. |
| Oracle real | Migration, grafo, nova conexão, segunda aplicação, constraints, queries decimais e restart aprovados. |
| Mongo real | Round-trip, isolamento, filtros, páginas, empate de datas, índice, novo contexto e restart aprovados. |
| Docker | Build real, runtime sem privilégios e restart da API aprovados com override privado descrito abaixo. |
| Compose | Principal, testes e principal + observabilidade válidos; revalidados após restringir os bindings a loopback. |
| Pacotes vulneráveis | Zero advisories conhecidos no relatório final com dependências transitivas incluídas. |
| Diff / originais | Verificação de espaços, arquivos novos e segredos; `script.sql` e `script_bd.sql` preservados byte a byte. |

São **438 casos distintos**, não 876 testes distintos por somar Debug e Release. As coletas finais de 26/10 estão em `artifacts/audit3/TestResults/debug` e `release`; as de 25/10 permanecem em `artifacts/audit2/TestResults/validated-debug` e `validated-release`. Reproduções antigas com falhas/skips foram preservadas em diretórios separados e não entram nessa contagem. Sem os opt-ins de banco real documentados, esses testes ficam explicitamente ignorados; isso não ocorreu nas execuções finais.

### Cobertura e qualidade da evidência

| Assembly | Linhas cobertas / instrumentadas | Cobertura de linhas |
|---|---:|---:|
| Domain | 150 / 150 | 100,00% |
| Application | 854 / 857 | 99,65% |
| Infrastructure | 1536 / 1624 | 94,58% |
| API | 797 / 922 | 86,44% |

O [resumo de cobertura](coverage-summary.md) une pontos de linha por arquivo nos dois XML Cobertura únicos, removendo cópias idênticas anexadas aos TRX por hash. Não soma percentuais e não mistura coletas anteriores. Não foram acrescentadas exclusões para elevar o resultado. A métrica inclui DTOs/propriedades instrumentadas, não une branches e não comprova qualidade de assertion ou compatibilidade de provider. As três linhas de Application não cobertas permanecem visíveis.

As suítes HTTP comuns usam SQLite em memória e repositório documental em memória, explicitamente declarados na factory. Não são apresentadas como evidência Oracle/Mongo. Testes opt-in usam os providers concretos e bancos próprios; os fluxos nativos abaixo complementam esses testes.

### HTTP, segurança e observabilidade

O processo nativo, em `127.0.0.1:5187`, executou login e os sete CRUDs, leitura após escrita, `Location`, links navegáveis, filtros, páginas, ordem decimal, precisão, evolução com autor do JWT e leitura do histórico após excluir o pet. Também conferiu 400 de validação/período/ordem/página, 401 sem token, 403 sem escopo e 404. A distribuição foi: 200 × 45; 201 × 11; 204 × 9; 400 × 7; 401 × 7; 403 × 1; 404 × 10. O roteiro de 25/10 foi repetido em 26/10 na cópia limpa com os mesmos resultados; o relatório bruto atual está em `artifacts/audit3/native-http-report.json`, resumido em `docs/validation/sprint4-results.json`. O processo foi encerrado e apenas os registros relacionais criados pelo roteiro foram removidos.

O OpenAPI servido foi salvo nas duas coletas (`artifacts/audit2/openapi-v1.json` e `artifacts/audit3/openapi-v1.json`): 34 paths, referências locais resolvidas, Bearer, schemas e defaults de paginação/ordem conferidos. As suítes adicionais exercitam 500/503 genéricos, indisponibilidade Oracle/Mongo, health agregado e correlação, sem expor connection strings ou detalhes internos no corpo. Serilog JSON e arquivo foram exercitados, assim como propagação de tracing entre requisição, Service e repositório. O coletor aguarda encerramento do span da própria requisição.

A revisão de segredos nos arquivos entregáveis não encontrou padrões de chaves privadas ou tokens GitHub/AWS/OpenAI. Exemplos usam placeholders; `.env`, logs, resultados, outputs e credenciais temporárias não são versionados nem entram no contexto Docker. Isso é uma verificação de padrões e cenários testados, não uma garantia sobre todo dado futuro fornecido à aplicação.

O relatório NuGet foi novamente gerado em `artifacts/audit3/packages-final.json`; os relatórios antes/depois da correção permanecem em `artifacts/audit2`. Zero advisories conhecidos nessa consulta não significa ausência de vulnerabilidades desconhecidas. O runtime nativo disponível permaneceu em 9.0.19; a imagem Docker validada em 25/10 reportou ASP.NET Core/.NET 9.0.22. Não foi feita instalação global de runtime.

## Oracle: aplicação real e compatibilidade

A restrição inicial do sandbox para WSL não demonstrava inexistência de infraestrutura. A inspeção autorizada encontrou Ubuntu WSL 2; um Docker Engine oficial 29.9.2 temporário, sem instalação de serviço, permitiu iniciar Oracle XE 21 em container novo. A imagem foi `gvenzl/oracle-xe:21-slim`, digest `sha256:ecdf4302ac3d134e1bac5ef6e0c223c2d0f4d4d2b6d551aa79b2346f1ab8f792`. Em 25/10 usou-se exclusivamente o schema descartável `clyvovet_audit2`; em 26/10, outro container novo forneceu `clyvovet_test`. Ambos usaram credenciais próprias geradas para a auditoria, sem conexão com o banco do grupo.

A migration ativa é `20261025155040_OracleBaseline`. O teste real executou `Database.MigrateAsync`, confirmou o histórico e gravou o grafo das sete entidades, com três pets e três consultas. Um novo contexto/conexão reaplicou migrations sem recriação e confirmou preservação, busca, paginação, desempate por ID e ordem decimal de peso/valor no provider Oracle. Um status inválido foi rejeitado pelo check real. A limpeza removeu apenas raízes identificadas por IDs e marcador aleatório do teste, com seus cascades; não houve `EnsureDeleted`, `DROP` ou reset de schema.

Separadamente, um marcador confirmado e o histórico EF sobreviveram ao restart do container Oracle. O marcador foi removido após a conferência. Evidências: `artifacts/audit2/oracle-real`, `oracle-restart-verified.txt` e `oracle-schema-inspection.txt`. A inspeção somente leitura, executada com o usuário da aplicação, confirmou **sete tabelas, 36 colunas de negócio, sete PKs, seis FKs com cascade, dois índices únicos de e-mail e sete identidades**. O snapshot está consistente; a CLI confirmou que não havia migration a aplicar novamente.

A baseline recusa schema com tabelas clínicas existentes antes de tentar criá-las. O caminho `Up` não executa remoções; o `Down` convencional é destrutivo, está documentado e não foi executado. As migrations da modelagem anterior estão arquivadas como texto em `docs/migrations-legadas`, fora da compilação. A compatibilidade SQL Oracle 21 é uniforme no runtime, design time e testes. A API não aplica migrations automaticamente na inicialização.

`script_bd.sql` original contém colunas de status com aspas/minúsculas e checks que usam nomes sem aspas, além de literais não duplicados dentro do SQL dinâmico. A baseline/DDL próprios do .NET corrigem esses pontos. **O original foi preservado.** `script.sql` representa uma modelagem anterior com `TUTORES`; também foi preservado e não deve ser combinado com a baseline atual de `DONOS`. Os dois arquivos continuam com hashes idênticos aos da entrada desta auditoria e diff vazio contra HEAD.

O [guia Oracle](database/README.md) separa criação do zero e adoção assistida. Um banco existente precisa de backup, comparação das colunas/identidades/constraints e histórico antes de qualquer registro de baseline; divergências exigem migration incremental específica. A inspeção não consome `NEXTVAL` nem altera sequências. O SQL completo exportado não é prometido como script idempotente de reaplicação. Nenhuma adoção, correção de histórico ou alteração no banco do grupo foi executada.

## MongoDB: finalidade e persistência real

Em 25/10, MongoDB Community 8.0.16 foi iniciado como processo temporário em loopback, sem serviço instalado. Em 26/10, os testes completos e o HTTP nativo foram repetidos contra MongoDB 8.0.34 em container descartável. Os sete testes reais usam o repositório concreto e bancos aleatórios próprios: escrita/leitura, isolamento por pet, tag/período, páginas/ordem, ping, formato e ordem do índice composto, empate de timestamps e round-trip de todos os campos UTC em novo contexto. A prova adicional de 25/10 em `artifacts/audit2/mongo-restart.json` confirmou documento e índice após shutdown/restart real, removendo somente seu banco de prova.

As evoluções são registros clínicos imutáveis com texto, data UTC, tags e autor derivado do JWT. Atualização e exclusão documental não fazem parte do contrato append-only. A criação consulta a existência do pet no Oracle e realiza uma escrita Mongo. Não existe transação distribuída: timeout pode deixar o resultado incerto e repetir a solicitação pode duplicar um registro. Excluir o pet relacional preserva seu histórico documental, acessível à conta clínica. Tags são deduplicadas sem diferenciar maiúsculas na entrada, mas o filtro Mongo corresponde ao valor armazenado; esse comportamento está documentado.

Esses limites são decisões explícitas de contrato e retenção, não uma promessa de consistência exatamente uma vez. Falha documental retorna 503. O fluxo nativo também comprovou a integração dos dois bancos, inclusive retenção após exclusão relacional.

## Docker e Compose: resultado e alcance

Os três arranjos de configuração passaram com Compose oficial 5.6.0 e checksum conferido. O Dockerfile foi efetivamente construído; a imagem final foi `sha256:decae6ce566ff1320bf7349ad6d802ec4614ef07ea7601acdcc28d776fc3e949`. O runtime usou usuário `app`, UID 1654; JSON e arquivo de log funcionaram. Antes e após restart da API: live 200, ready 200 com os dois bancos, OpenAPI 200 e GET clínico anônimo 401.

O daemon descartável foi deliberadamente iniciado sem forwarding/iptables. Na rede bridge original, live respondeu 200 e readiness 503. A tentativa de colocar a API em host networking foi **rejeitada pela revisão automática antes de executar**, por ampliar seu acesso à rede do host sem autorização específica. A validação prosseguiu por alternativa restrita: somente os três containers novos compartilharam o namespace privado do container Oracle, com publicações em `127.0.0.1`. Não houve alteração de firewall/sysctl.

Esse smoke não aplicou migrations no Oracle do projeto Compose: readiness comprova conexão, não tabelas. A aplicação e os CRUDs reais foram provados separadamente no schema Oracle da auditoria. Os containers/volumes do projeto de smoke foram removidos após conferência de IDs/labels. Evidências estão em `artifacts/audit2/docker-runtime/validation-summary.json`, logs sanitizados e resultados HTTP antes/depois do restart.

Depois do smoke, os bindings versionados de Oracle/Dashboard/OTLP foram restringidos a loopback. Os três arranjos foram revalidados por `config --quiet`, sem nova execução dos containers por essa alteração de binding. A comunicação entre serviços e a publicação da API 8080 não mudaram. **A execução na rede bridge padrão continua pendente em um daemon com rede habilitada.**

## Alinhamento necessário com André

- Conferir Oracle 21, schema atual, nomes/aspas das 36 colunas, sete tabelas, `DECIMAL(5,2)` de peso, `DECIMAL(10,2)` de valor, datas `TIMESTAMP(7)`, identidades e IDs existentes, checks e unicidade de e-mail. Revisar os defeitos encontrados no `script_bd.sql` antes de voltar a aplicá-lo.
- Decidir sobre adoção da baseline somente após backup/inspeção do schema existente. Um banco legado `TUTORES` não equivale ao atual `DONOS`; não marcar a baseline como aplicada se houver diferenças. Nenhum reset ou DROP é necessário para essa revisão.
- Confirmar os seis cascades: excluir tutor pode remover pets, consultas e vacinas; excluir funcionário pode remover consultas e prescrições. Alinhar a retenção do histórico Mongo que permanece após a exclusão relacional.
- Exercitar o Compose padrão com bridge em Docker Engine normal; confirmar DNS/serviços, volumes, health, permissões dos logs, restart e aplicação deliberada das migrations no schema novo. `APP_USER`/senhas de inicialização só criam o usuário em volume Oracle novo; mudar `.env` não altera automaticamente um banco já inicializado.
- Definir conexões privadas, senha Oracle compatível com XE 21 (até 30 caracteres ASCII nesse ambiente), chave JWT estável com pelo menos 32 bytes, usuário clínico, autenticação/TLS Mongo e runtime de destino atualizado. Os exemplos não são segredos de produção.
- Confirmar portas e exposição remota: bancos e Dashboard/OTLP agora publicam só em loopback; a API mantém 8080. Dashboard local permite acesso anônimo. Uma futura configuração Azure ou acesso remoto exige decisão explícita de Database/DevOps e controles próprios.

Não foram criadas integrações artificiais com Java/Mobile. Azure, Billing, Boards/QA, outros repositórios e infraestrutura compartilhada não foram alterados. Não foram produzidos vídeos.

## Arquivos alterados e entrega

O diff completo da Sprint 4 inclui a separação de Domain/Application/Infrastructure, Services/DTOs/repositórios movidos, migrations legadas arquivadas, novos contratos REST/Mongo, testes, Docker e documentação. A segunda auditoria de 25/10 acrescentou correções específicas sem outra reestruturação: **35 arquivos alterados e oito novos**, sem exclusões em relação à entrada daquela auditoria. Seu inventário está em `artifacts/audit2/changed-files.json`. Na última revisão de 26/10, foram ajustados somente README, guia Oracle e este relatório; foram acrescentados os dois JSON de `docs/validation`. Nenhum arquivo de produção/teste foi removido ou alterado nessa última rodada. A lista completa para o futuro commit é `docs/validation/git-delivery.json`.

| Grupo da segunda auditoria | Arquivos principais |
|---|---|
| Precisão | `src/ClyvoVetApi.Application/DTOs/Request/PetRequestDto.cs`, `ConsultaRequestDto.cs`, novo `DTOs/Validation/TwoDecimalPlacesAttribute.cs`; novos testes unitários/HTTP de precisão. |
| REST/OpenAPI | `Http/HateoasResultFilter.cs`, novo `Http/OpenApiContractTransformer.cs`, metadados dos oito controllers clínicos, `Program.cs`, novo `ClyvoVetApi.Tests.Integration/AuditRestRegressionTests.cs`. |
| Privacidade | `Controllers/AuthController.cs`, `Logging/SensitiveDataMasker.cs`, novos `Logging/SafeJsonFormatter.cs`, `Observability/SafeTelemetryProcessor.cs`, configuração Serilog e testes `TelemetryPrivacyTests.cs`/mascaramento. |
| Persistência/testes | `MongoRealTests.cs`, `OraclePersistenceTests.cs`, `SpanCollector.cs`, `TracingTests.cs`, projetos com versões de pacotes corrigidas. |
| Operação/documentação | `.env.example`, dois Compose com bindings, `scripts/Export-OracleSql.ps1`, `scripts/coverage-summary.ps1`, SQL exportado/inspeção em `docs/database`, README, guia Oracle, resumo de cobertura e este relatório. |

A revisão final verificou os arquivos novos além do diff rastreado: nenhum problema de espaços, UTF-8 ou links Markdown relativos; nenhum fonte C#, projeto ou solução mudou depois dos testes finais. Não houve staging, commit ou push. `script.sql` e `script_bd.sql` continuam intactos. O Mongo temporário, o diretório de dados do Docker/Oracle descartável e suas credenciais privadas foram removidos após preservar as evidências sanitizadas (`mongo-cleanup.json` e `docker-cleanup.json`). Nenhum serviço de banco ou Docker foi instalado no Windows. A limpeza foi limitada aos caminhos, marcadores e processos pertencentes à auditoria; o Ubuntu WSL existente foi preservado.

As penalidades por projeto sem compilar, README ausente e ausência de testes não foram observadas. Os requisitos de migrations reais e Mongo funcional agora têm prova local concreta. Permanecem o teste de rede bridge padrão e as decisões de adoção/integração com André, identificados acima para revisão antes de qualquer commit.
