$ErrorActionPreference = 'Stop'
$baseUrl = 'http://127.0.0.1:8765/api/report'
$backupPath = Join-Path $env:TEMP ("utility-main-etag-backup-" + [guid]::NewGuid().ToString('N') + '.html')
$backupCreated = $false
$reportModified = $false
$curlConfigFiles = New-Object System.Collections.Generic.List[string]

function Get-StatusCode($response) {
    if ($response.Count -eq 0 -or $response[0] -notmatch '^HTTP/\S+\s+(\d{3})') {
        throw "Не удалось прочитать HTTP-статус: $response"
    }
    return [int]$Matches[1]
}

function Get-ETag($response) {
    $line = $response | Where-Object { $_ -match '(?i)^ETag:\s*(.+)$' } | Select-Object -First 1
    if (-not $line) {
        throw 'В ответе отсутствует ETag. Сначала сгенерируйте HTML-отчёт в приложении.'
    }
    [void]($line -match '(?i)^ETag:\s*(.+)$')
    return $Matches[1].Trim()
}

function New-CurlHeaderConfig([string]$name, [string]$value) {
    # Windows PowerShell 5.1 can strip embedded double quotes from native
    # command arguments. A curl config file preserves the exact ETag syntax.
    $configPath = Join-Path $env:TEMP ("curl-etag-header-" + [guid]::NewGuid().ToString('N') + '.conf')
    $escapedValue = $value.Replace('\', '\\').Replace('"', '\"')
    $configLine = 'header = "' + $name + ': ' + $escapedValue + '"'
    [System.IO.File]::WriteAllText($configPath, $configLine + [Environment]::NewLine, [System.Text.Encoding]::ASCII)
    $curlConfigFiles.Add($configPath)
    return $configPath
}

try {
    Write-Host 'Сохраняю исходный отчёт, чтобы вернуть его после тестов...'
    $initialHeaders = & curl.exe -sS -D - -o NUL $baseUrl
    if ($LASTEXITCODE -ne 0) { throw 'Не удалось подключиться к ETag API' }
    $initialEtag = Get-ETag $initialHeaders

    & curl.exe -sS -o $backupPath $baseUrl
    if ($LASTEXITCODE -ne 0) { throw 'Не удалось создать резервную копию отчёта' }
    $backupCreated = $true

    Write-Host '1. Проверка GET и получения ETag'
    $getResponse = & curl.exe -sS -i $baseUrl
    if ($LASTEXITCODE -ne 0) { throw 'curl завершился с ошибкой при GET' }
    $getStatus = Get-StatusCode $getResponse
    if ($getStatus -ne 200) { throw "Ожидался GET 200, получен $getStatus" }
    $etag = Get-ETag $getResponse
    Write-Host "PASS: GET $getStatus, ETag = $etag" -ForegroundColor Green

    Write-Host '2. Проверка If-None-Match'
    $ifNoneMatchConfig = New-CurlHeaderConfig 'If-None-Match' $etag
    $notModified = & curl.exe --config $ifNoneMatchConfig -sS -o NUL -w '%{http_code}' $baseUrl
    if ($LASTEXITCODE -ne 0) { throw 'curl завершился с ошибкой при If-None-Match' }
    if ([string]$notModified -ne '304') { throw "Ожидался 304, получен $notModified" }
    Write-Host 'PASS: If-None-Match вернул 304 Not Modified' -ForegroundColor Green

    Write-Host '3. Проверка обязательного If-Match'
    $missingIfMatch = & curl.exe -sS -o NUL -w '%{http_code}' -X PUT -H 'Content-Type: text/html' --data-binary '<h1>Must not be saved</h1>' $baseUrl
    if ($LASTEXITCODE -ne 0) { throw 'curl завершился с ошибкой при PUT без If-Match' }
    if ([string]$missingIfMatch -ne '428') { throw "Ожидался 428, получен $missingIfMatch" }
    Write-Host 'PASS: PUT без If-Match вернул 428 Precondition Required' -ForegroundColor Green

    Write-Host '4. Проверка успешного обновления клиентом A'
    $ifMatchConfig = New-CurlHeaderConfig 'If-Match' $etag
    $clientAResponse = & curl.exe --config $ifMatchConfig -sS -i -X PUT -H 'Content-Type: text/html' --data-binary '<h1>Update from client A</h1>' $baseUrl
    if ($LASTEXITCODE -ne 0) { throw 'curl завершился с ошибкой при PUT клиента A' }
    $clientAStatus = Get-StatusCode $clientAResponse
    if ($clientAStatus -ne 200) { throw "Ожидался PUT клиента A 200, получен $clientAStatus" }
    $reportModified = $true
    $newEtag = Get-ETag $clientAResponse
    if ($newEtag -eq $etag) { throw 'После изменения содержимого ETag не изменился' }
    Write-Host "PASS: клиент A обновил отчёт, новый ETag = $newEtag" -ForegroundColor Green

    Write-Host '5. Проверка защиты от устаревшей версии клиента B'
    $staleIfMatchConfig = New-CurlHeaderConfig 'If-Match' $etag
    $staleIfMatch = & curl.exe --config $staleIfMatchConfig -sS -o NUL -w '%{http_code}' -X PUT -H 'Content-Type: text/html' --data-binary '<h1>Update from client B</h1>' $baseUrl
    if ($LASTEXITCODE -ne 0) { throw 'curl завершился с ошибкой при PUT клиента B' }
    if ([string]$staleIfMatch -ne '412') { throw "Ожидался 412, получен $staleIfMatch" }
    Write-Host 'PASS: старый ETag клиента B отклонён с 412' -ForegroundColor Green

    Write-Host '6. Проверка обновления с актуальным ETag'
    $currentIfMatchConfig = New-CurlHeaderConfig 'If-Match' $newEtag
    $putResponse = & curl.exe --config $currentIfMatchConfig -sS -i -X PUT -H 'Content-Type: text/html' --data-binary '<h1>ETag test update</h1>' $baseUrl
    if ($LASTEXITCODE -ne 0) { throw 'curl завершился с ошибкой при PUT с актуальным ETag' }
    $putStatus = Get-StatusCode $putResponse
    if ($putStatus -ne 200) { throw "Ожидался PUT 200, получен $putStatus" }
    $reportModified = $true
    $finalEtag = Get-ETag $putResponse
    if ($finalEtag -eq $newEtag) { throw 'После второго изменения содержимого ETag не изменился' }
    Write-Host "PASS: актуальный If-Match принят, новый ETag = $finalEtag" -ForegroundColor Green

    $updatedContent = & curl.exe -sS $baseUrl
    if ($LASTEXITCODE -ne 0) { throw 'curl завершился с ошибкой при повторном GET' }
    if (-not ([string]$updatedContent).Contains('ETag test update')) {
        throw 'Повторный GET не вернул обновлённое содержимое'
    }
    Write-Host 'PASS: повторный GET вернул обновлённый отчёт' -ForegroundColor Green
    Write-Host 'Все curl-тесты ETag пройдены.' -ForegroundColor Green
}
finally {
    # Restore only if a PUT test actually changed the report. If a test failed
    # before the first successful PUT, the original report is already intact.
    if ($reportModified -and $backupCreated -and (Test-Path $backupPath)) {
        try {
            $restoreHeaders = & curl.exe -sS -i $baseUrl
            if ($LASTEXITCODE -ne 0) { throw 'ETag API недоступен для восстановления отчёта' }
            $restoreEtag = Get-ETag $restoreHeaders
            $restoreConfig = New-CurlHeaderConfig 'If-Match' $restoreEtag
            $restoreStatus = & curl.exe --config $restoreConfig -sS -o NUL -w '%{http_code}' -X PUT -H 'Content-Type: text/html' --data-binary "@$backupPath" $baseUrl
            if ($LASTEXITCODE -ne 0 -or [string]$restoreStatus -ne '200') {
                throw "Сервер вернул статус $restoreStatus при восстановлении"
            }
            Remove-Item $backupPath -Force
            Write-Host 'Исходный отчёт восстановлен.' -ForegroundColor Green
        }
        catch {
            Write-Warning "Не удалось автоматически восстановить отчёт. Резервная копия сохранена: $backupPath. Ошибка: $_"
        }
    }
    elseif ($backupCreated -and (Test-Path $backupPath)) {
        Remove-Item $backupPath -Force
    }

    foreach ($configFile in $curlConfigFiles) {
        if (Test-Path $configFile) { Remove-Item $configFile -Force }
    }
}
