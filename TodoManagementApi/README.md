# Todo Management API

Учебный проект на C# / ASP.NET Core по примеру OpenAPI из предоставленного файла.

## Реализовано

- Swagger / OpenAPI с названием `Todo Management API`.
- Версия документации `v1`.
- Описание, контактная информация и лицензия MIT.
- `GET /api/todos` — список задач.
- `GET /api/todos?completed=false` — фильтрация.
- `GET /api/todos/{id}` — задача по ID.
- `POST /api/todos` — создание задачи.

Данные хранятся в памяти и очищаются после остановки приложения.

## Требования

Установленный .NET 8 SDK. Проверка:

```powershell
dotnet --version
```

## Автоматическая проверка

Открой PowerShell в папке проекта и выполни:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\verify.ps1
```

Скрипт восстанавливает пакеты, собирает проект, запускает API, проверяет POST, GET, фильтрацию и Swagger JSON, после чего останавливает тестовый сервер.

## Обычный запуск

```powershell
dotnet restore
dotnet build
dotnet run --urls http://localhost:5080
```

Открой в браузере:

http://localhost:5080/swagger

## Важно

Скрипт `verify.ps1` содержит только ASCII-символы, чтобы Windows PowerShell 5.1 не ломал русские строки из-за кодировки файла.
