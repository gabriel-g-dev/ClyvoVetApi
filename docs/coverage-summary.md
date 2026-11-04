# Cobertura da Sprint 4

## Coleta de 04/11/2026

Coleta local em Debug: 197 testes unitários e 248 testes de integração aprovados, sem falhas. Os 8 testes com Oracle/MongoDB reais foram ignorados por ausência de bancos descartáveis configurados.

União de linhas executadas em 2 relatórios Cobertura da mesma execução, gerados pelas suítes xUnit.

| Assembly | Linhas cobertas | Linhas instrumentadas | Cobertura |
|---|---:|---:|---:|
| ClyvoVetApi.Domain | 150 | 150 | 100.00% |
| ClyvoVetApi.Application | 854 | 857 | 99.65% |
| ClyvoVetApi.Infrastructure | 1510 | 1624 | 92.98% |
| ClyvoVetApi | 837 | 930 | 90.00% |

## Coleta anterior de 02/11/2026

Coleta local de 02/11/2026 em Debug: 197 testes unitários e 233 testes de integração aprovados. Os 8 testes com Oracle/MongoDB reais foram ignorados por ausência de bancos descartáveis configurados.

União de linhas executadas em 2 relatórios Cobertura, gerados pelas suítes xUnit.

| Assembly | Linhas cobertas | Linhas instrumentadas | Cobertura |
|---|---:|---:|---:|
| ClyvoVetApi.Domain | 150 | 150 | 100.00% |
| ClyvoVetApi.Application | 854 | 857 | 99.65% |
| ClyvoVetApi.Infrastructure | 1471 | 1624 | 90.58% |
| ClyvoVetApi | 797 | 922 | 86.44% |

A métrica considera linhas executáveis instrumentadas pelo Coverlet. DTOs e propriedades automáticas podem ter poucos pontos instrumentados.
A cobertura não demonstra compatibilidade Oracle nem mede qualidade das assertions. Testes Oracle ignorados não contam como execução real.
Os XML originais também apresentam branches por suíte; este resumo une somente linhas, sem somar percentuais de branches.
