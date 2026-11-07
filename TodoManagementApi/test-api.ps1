$ErrorActionPreference = "Stop"
$baseUrl = "http://localhost:5080"

Write-Host "Создание первой задачи..."
$body1 = @{ title = "Изучить OpenAPI и Swagger" } | ConvertTo-Json
Invoke-RestMethod -Uri "$baseUrl/api/todos" -Method Post -ContentType "application/json" -Body $body1

Write-Host "`nСоздание второй задачи..."
$body2 = @{ title = "Проверить работу фильтрации" } | ConvertTo-Json
Invoke-RestMethod -Uri "$baseUrl/api/todos" -Method Post -ContentType "application/json" -Body $body2

Write-Host "`nПолучение всех задач..."
Invoke-RestMethod -Uri "$baseUrl/api/todos" -Method Get | Format-Table

Write-Host "`nПолучение незавершённых задач..."
Invoke-RestMethod -Uri "$baseUrl/api/todos?completed=false" -Method Get | Format-Table

Write-Host "`nПроверка завершена."
