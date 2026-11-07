$ErrorActionPreference = "Stop"

$projectDir = $PSScriptRoot
$baseUrl = "http://localhost:5080"
$stdoutFile = Join-Path $env:TEMP "todo-api-stdout.log"
$stderrFile = Join-Path $env:TEMP "todo-api-stderr.log"
$server = $null

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host ("=== " + $Message + " ===") -ForegroundColor Cyan
}

try {
    Set-Location $projectDir

    Write-Step "Checking .NET SDK"
    dotnet --version
    if ($LASTEXITCODE -ne 0) { throw ".NET SDK is not installed or is not working." }

    Write-Step "Restoring packages"
    dotnet restore
    if ($LASTEXITCODE -ne 0) { throw "Package restore failed." }

    Write-Step "Building project"
    dotnet build --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }

    Write-Step "Starting API server"
    if (Test-Path $stdoutFile) { Remove-Item $stdoutFile -Force }
    if (Test-Path $stderrFile) { Remove-Item $stderrFile -Force }

    $server = Start-Process -FilePath "dotnet" `
        -ArgumentList @("run", "--no-build", "--no-launch-profile", "--urls", $baseUrl) `
        -WorkingDirectory $projectDir `
        -PassThru `
        -WindowStyle Hidden `
        -RedirectStandardOutput $stdoutFile `
        -RedirectStandardError $stderrFile

    $ready = $false
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Seconds 1
        try {
            $null = Invoke-RestMethod -Uri "$baseUrl/api/todos" -Method Get -TimeoutSec 2
            $ready = $true
            break
        }
        catch {
            if ($server.HasExited) { break }
        }
    }

    if (-not $ready) {
        if (Test-Path $stderrFile) { Get-Content $stderrFile }
        if (Test-Path $stdoutFile) { Get-Content $stdoutFile }
        throw "API server did not start at $baseUrl."
    }

    Write-Host ("SERVER RUNNING: " + $baseUrl) -ForegroundColor Green

    Write-Step "Testing POST /api/todos"
    $payload = @{ title = "Check OpenAPI and Swagger" } | ConvertTo-Json
    $created = Invoke-RestMethod -Uri "$baseUrl/api/todos" `
        -Method Post -ContentType "application/json" -Body $payload
    $created | Format-List
    if (-not $created.id) { throw "POST test failed: no task ID returned." }
    Write-Host "POST /api/todos: OK" -ForegroundColor Green

    Write-Step "Testing GET /api/todos"
    $all = @(Invoke-RestMethod -Uri "$baseUrl/api/todos" -Method Get)
    $all | Format-Table
    if ($all.Count -lt 1) { throw "GET test failed: task list is empty." }
    Write-Host "GET /api/todos: OK" -ForegroundColor Green

    Write-Step "Testing GET /api/todos?completed=false"
    $filtered = @(Invoke-RestMethod -Uri "$baseUrl/api/todos?completed=false" -Method Get)
    if ($filtered.Count -lt 1) { throw "Filter test failed." }
    Write-Host "FILTER: OK" -ForegroundColor Green

    Write-Step "Testing Swagger document"
    $swagger = Invoke-RestMethod -Uri "$baseUrl/swagger/v1/swagger.json" -Method Get
    if ($swagger.info.title -ne "Todo Management API") {
        throw "Swagger document does not contain the expected API title."
    }
    Write-Host "SWAGGER DOCUMENT: OK" -ForegroundColor Green

    Write-Host ""
    Write-Host "RESULT: BUILD AND API TESTS PASSED" -ForegroundColor Green
    Write-Host ("Swagger UI: " + $baseUrl + "/swagger") -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host ("ERROR: " + $_.Exception.Message) -ForegroundColor Red
    exit 1
}
finally {
    if ($null -ne $server -and -not $server.HasExited) {
        Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue
    }
}
