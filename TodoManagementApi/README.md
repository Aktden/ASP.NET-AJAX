# Todo Management API

<img width="1920" height="1080" alt="WindowsTerminal_rBUJJEA4iB" src="https://github.com/user-attachments/assets/dbe76fc8-72a9-4c6d-9832-ace3350f2e54" />

<img width="1920" height="1080" alt="WindowsTerminal_H6jyP5ehOr" src="https://github.com/user-attachments/assets/69fb6c6d-006e-43c7-a2a1-15c66fa007f8" />

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
