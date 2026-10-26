param([switch]$NoBuild)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$infraProject = Join-Path $repoRoot 'src/ClyvoVetApi.Infrastructure/ClyvoVetApi.Infrastructure.csproj'
$outputDirectory = Join-Path $repoRoot 'docs/database'
$sqlEncoding = New-Object System.Text.UTF8Encoding($false)
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

Push-Location $repoRoot
try {
    if (-not $NoBuild) {
        dotnet tool restore
        if ($LASTEXITCODE -ne 0) { throw 'Falha ao restaurar dotnet-ef.' }
    }

    $migrationsPath = Join-Path $outputDirectory 'oracle-migrations.sql'
    if ($NoBuild) {
        dotnet ef migrations script --no-build --project $infraProject --startup-project $infraProject --output $migrationsPath
    }
    else {
        dotnet ef migrations script --project $infraProject --startup-project $infraProject --output $migrationsPath
    }
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao exportar migrations.' }
    $migrationSql = Get-Content -LiteralPath $migrationsPath -Raw
    $migrationSql = [regex]::Replace($migrationSql, '(?m)[ \t]+(?=\r?$)', '')
    [System.IO.File]::WriteAllText($migrationsPath,
        (("WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK`n" + $migrationSql + "`nCOMMIT`n/").TrimEnd() + "`n"), $sqlEncoding)

    $schemaPath = Join-Path $outputDirectory 'oracle-schema.sql'
    dotnet ef dbcontext script --no-build --project $infraProject --startup-project $infraProject --output $schemaPath
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao exportar DDL.' }
    # Oracle EF 9.23.60 não escapa os literais dos checks no SQL dinâmico.
    $schemaSql = Get-Content -LiteralPath $schemaPath -Raw
    $schemaSql = $schemaSql.Replace("IN ('A', 'C', 'R')", "IN (''A'', ''C'', ''R'')")
    $schemaSql = $schemaSql.Replace("IN ('P', 'A')", "IN (''P'', ''A'')")
    $schemaSql = [regex]::Replace($schemaSql, '(?m)([^\r\n])/\r?$', '$1' + "`n/")
    $schemaSql = [regex]::Replace($schemaSql, '(?m)[ \t]+(?=\r?$)', '')
    [System.IO.File]::WriteAllText($schemaPath,
        (("WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK`n" + $schemaSql).TrimEnd() + "`n"), $sqlEncoding)
}
finally {
    Pop-Location
}
