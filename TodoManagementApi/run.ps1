$ErrorActionPreference = "Stop"

Write-Host "=== Todo Management API ===" -ForegroundColor Cyan
Write-Host "Проверка установленного .NET SDK..." -ForegroundColor Yellow
dotnet --version

Write-Host "`nВосстановление пакетов..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) { throw "Не удалось восстановить пакеты." }

Write-Host "`nСборка проекта..." -ForegroundColor Yellow
dotnet build --no-restore
if ($LASTEXITCODE -ne 0) { throw "Ошибка сборки проекта." }

Write-Host "`nПроект собран. Запуск сервера..." -ForegroundColor Green
Write-Host "Swagger: http://localhost:5080/swagger" -ForegroundColor Green
dotnet run --no-build --urls http://localhost:5080
