# AuthDemo — примеры аутентификации ASP.NET Core

Учебный проект по материалам Markdown-файла. В решении собраны четыре подхода:

1. Cookie Authentication.
2. JWT Bearer с Access Token и Refresh Token.
3. BFF (Backend for Frontend).
4. Combined Authentication — автоматический выбор Cookie или Bearer.

Проект разделён на два приложения:

- `AuthDemo.Api` — API, выпуск и проверка JWT, Cookie-аутентификация и комбинированная схема.
- `AuthDemo.Bff` — BFF-сервер, который хранит токены на сервере и отдаёт браузеру только HttpOnly cookie.

## Требования

- .NET 8 SDK или новее.
- Два свободных локальных порта: `5080` и `5081`.

## Запуск

Открой два терминала в корневой папке решения.

Терминал 1 — API:

```bash
dotnet run --project AuthDemo.Api --launch-profile http
```

Терминал 2 — BFF:

```bash
dotnet run --project AuthDemo.Bff --launch-profile http
```

Адреса:

- API: http://localhost:5080
- BFF: http://localhost:5081

Проверка сборки:

```bash
dotnet restore
dotnet build
```

## Тестовые учётные записи

Только для локальной демонстрации:

| Логин | Пароль | Роли |
|---|---|---|
| `alice` | `p@ss` | `user`, `admin` |
| `bob` | `p@ss` | `user` |

Пароли в примере хранятся не в открытом виде: используется PBKDF2 с индивидуальной солью. Для реального приложения вместо демонстрационного хранилища подключите ASP.NET Core Identity или свою базу пользователей.

---

## 1. Cookie Authentication

Сначала получите CSRF-токен:

```bash
curl -i -c cookies.txt http://localhost:5080/cookie/csrf
```

Из JSON-ответа скопируйте `requestToken`. Затем выполните вход, передав cookie-файл и токен в заголовке:

```bash
curl -i -b cookies.txt -c cookies.txt \
  -H "Content-Type: application/json" \
  -H "X-CSRF-TOKEN: ВСТАВЬТЕ_REQUEST_TOKEN" \
  -d "{\"username\":\"alice\",\"password\":\"p@ss\",\"rememberMe\":false}" \
  http://localhost:5080/cookie/login
```

Проверка текущего пользователя:

```bash
curl -i -b cookies.txt http://localhost:5080/cookie/me
```

Проверка доступа администратора:

```bash
curl -i -b cookies.txt http://localhost:5080/cookie/admin
```

Выход выполняется через `POST /cookie/logout` с CSRF-токеном. При выходе сервер отзывает идентификатор сессии.

## 2. JWT Bearer

Получите пару токенов:

```bash
curl -i -H "Content-Type: application/json" \
  -d "{\"username\":\"alice\",\"password\":\"p@ss\"}" \
  http://localhost:5080/token
```

В ответе будут:

- `accessToken` — короткоживущий JWT (15 минут).
- `refreshToken` — токен обновления (7 дней).
- `expiresIn` — срок действия Access Token в секундах.

Проверка защищённого API:

```bash
curl -i -H "Authorization: Bearer ВСТАВЬТЕ_ACCESS_TOKEN" \
  http://localhost:5080/api/me
```

Проверка роли администратора:

```bash
curl -i -H "Authorization: Bearer ВСТАВЬТЕ_ACCESS_TOKEN" \
  http://localhost:5080/api/admin
```

Обновление токенов:

```bash
curl -i -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"ВСТАВЬТЕ_REFRESH_TOKEN\"}" \
  http://localhost:5080/token/refresh
```

Refresh Token одноразовый: после успешного обновления старый токен отзывается, а клиент получает новую пару. В хранилище сохраняется SHA-256-хэш Refresh Token, а не его исходное значение.

## 3. BFF

BFF сам обращается к API за JWT. Браузер не получает Access Token и Refresh Token — только HttpOnly cookie BFF-сессии.

Получите CSRF-токен:

```bash
curl -i -c bff-cookies.txt http://localhost:5081/bff/csrf
```

Выполните вход:

```bash
curl -i -b bff-cookies.txt -c bff-cookies.txt \
  -H "Content-Type: application/json" \
  -H "X-CSRF-TOKEN: ВСТАВЬТЕ_REQUEST_TOKEN" \
  -d "{\"username\":\"alice\",\"password\":\"p@ss\"}" \
  http://localhost:5081/bff/login
```

Запрос профиля через BFF:

```bash
curl -i -b bff-cookies.txt http://localhost:5081/bff/me
```

Запрос административного ресурса через BFF:

```bash
curl -i -b bff-cookies.txt http://localhost:5081/bff/admin
```

BFF автоматически обновляет Access Token при приближении срока его истечения. При выходе BFF удаляет серверную сессию и отправляет запрос на отзыв Refresh Token в API.

## 4. Combined Authentication

API автоматически выбирает схему:

- если в запросе есть `Authorization: Bearer ...`, используется JWT Bearer;
- если Bearer-заголовка нет, используется Cookie.

Проверка:

```bash
curl -i -b cookies.txt http://localhost:5080/combined/me
```

или:

```bash
curl -i -H "Authorization: Bearer ВСТАВЬТЕ_ACCESS_TOKEN" \
  http://localhost:5080/combined/me
```

Административный endpoint:

```bash
curl -i -H "Authorization: Bearer ВСТАВЬТЕ_ACCESS_TOKEN" \
  http://localhost:5080/combined/admin
```

## Основные маршруты

| Метод | Маршрут | Назначение |
|---|---|---|
| GET | `/cookie/csrf` | Получить CSRF-токен для Cookie API |
| POST | `/cookie/login` | Вход по Cookie |
| POST | `/cookie/logout` | Выход и отзыв сессии |
| GET | `/cookie/me` | Профиль по Cookie |
| GET | `/cookie/admin` | Ресурс только для admin по Cookie |
| POST | `/token` | Выпуск JWT и Refresh Token |
| POST | `/token/refresh` | Ротация токенов |
| POST | `/token/revoke` | Отзыв Refresh Token |
| POST | `/token/revoke-access` | Отзыв текущего Access Token |
| GET | `/api/me` | Профиль по Bearer |
| GET | `/api/admin` | Ресурс только для admin по Bearer |
| GET | `/combined/me` | Профиль по Cookie или Bearer |
| GET | `/combined/admin` | Админ-ресурс по Cookie или Bearer |
| GET | `/bff/csrf` | Получить CSRF-токен для BFF |
| POST | `/bff/login` | Вход через BFF |
| GET | `/bff/me` | Профиль через BFF |
| GET | `/bff/admin` | Админ-ресурс через BFF |
| POST | `/bff/logout` | Выход из BFF |

## Важные ограничения демонстрационного проекта

- Хранилища пользователей, сессий и токенов находятся в памяти процесса. После перезапуска данные сбрасываются.
- Для нескольких экземпляров приложения нужны общее постоянное хранилище и распределённая защита от повторного использования Refresh Token.
- Ключ JWT в `appsettings.Development.json` предназначен только для локального запуска. В production задайте секрет через переменную окружения `Jwt__Key` или Secret Manager.
- В production обязательно используйте HTTPS. В коде для Production cookie имеют `SecurePolicy.Always`.
- Для реальной эксплуатации добавьте ограничение частоты попыток входа, аудит, управление пользователями и безопасное хранение секретов.
- BFF хранит токены в памяти для наглядности. В production используйте защищённое серверное хранилище (например, Redis/БД с шифрованием). Ключи ASP.NET Core Data Protection сохраняются в локальных папках `keys/`; эти папки исключены из Git. Для нескольких экземпляров приложения настройте общее постоянное хранилище ключей.
- Пример не заменяет полноценную конфигурацию ASP.NET Core Identity или внешнего провайдера OpenID Connect.

## Структура

```text
AuthDemo/
├── AuthDemo.sln
├── README.md
├── .gitignore
├── AuthDemo.Api/
│   ├── AuthDemo.Api.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Properties/
│       └── launchSettings.json
└── AuthDemo.Bff/
    ├── AuthDemo.Bff.csproj
    ├── Program.cs
    ├── appsettings.json
    └── Properties/
        └── launchSettings.json
```
