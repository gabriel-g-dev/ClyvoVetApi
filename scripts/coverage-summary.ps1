param(
    [Parameter(Mandatory = $true)]
    [string]$ResultsDirectory,
    [string]$OutputPath = "artifacts/coverage-summary.md"
)

$ErrorActionPreference = "Stop"
$reports = @(Get-ChildItem -LiteralPath $ResultsDirectory -Recurse -Filter coverage.cobertura.xml)
if ($reports.Count -eq 0) { throw "Nenhum relatório Cobertura encontrado em $ResultsDirectory." }
$reports = @($reports | Group-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } |
    ForEach-Object { $_.Group[0] })

# A união considera cada linha de cada arquivo uma vez, mesmo quando coberta pelas duas suítes.
$lines = @{}
foreach ($report in $reports) {
    [xml]$coverage = Get-Content -LiteralPath $report.FullName -Raw
    foreach ($package in $coverage.coverage.packages.package) {
        foreach ($class in $package.classes.class) {
            $file = ([string]$class.filename).Replace('\', '/')
            foreach ($line in $class.lines.line) {
                $key = "$($package.name)|$file|$($line.number)"
                $lines[$key] = [bool]($lines[$key] -or [int]$line.hits -gt 0)
            }
        }
    }
}

$rows = foreach ($assembly in @('ClyvoVetApi.Domain', 'ClyvoVetApi.Application', 'ClyvoVetApi.Infrastructure', 'ClyvoVetApi')) {
    $keys = @($lines.Keys | Where-Object { $_.StartsWith("$assembly|") })
    $covered = @($keys | Where-Object { $lines[$_] }).Count
    $percentage = if ($keys.Count) { (100 * $covered / $keys.Count).ToString('F2', [cultureinfo]::InvariantCulture) } else { '0.00' }
    "| $assembly | $covered | $($keys.Count) | $percentage% |"
}
$content = @(
    '# Cobertura da Sprint 4', '',
    "União de linhas executadas em $($reports.Count) relatórios Cobertura, gerados pelas suítes xUnit.", '',
    '| Assembly | Linhas cobertas | Linhas instrumentadas | Cobertura |',
    '|---|---:|---:|---:|'
) + $rows + @('',
    'A métrica considera linhas executáveis instrumentadas pelo Coverlet. DTOs e propriedades automáticas podem ter poucos pontos instrumentados.',
    'A cobertura não demonstra compatibilidade Oracle nem mede qualidade das assertions. Testes Oracle ignorados não contam como execução real.',
    'Os XML originais também apresentam branches por suíte; este resumo une somente linhas, sem somar percentuais de branches.')
$parent = Split-Path -Parent $OutputPath
if ($parent) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
[IO.File]::WriteAllLines([IO.Path]::GetFullPath($OutputPath), $content, [Text.UTF8Encoding]::new($false))
$content
