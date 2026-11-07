using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<BffSessionStore>();
builder.Services.AddSingleton<BffTokenService>();

var bffKeyDirectory = Path.Combine(builder.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(bffKeyDirectory);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(bffKeyDirectory))
    .SetApplicationName("AuthDemo.Bff");

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = ".AuthDemo.Bff.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services.AddHttpClient("api", client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5080";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = ".AuthDemo.Bff.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/bff/login";

        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = context =>
            {
                var sid = context.Principal?.FindFirst("sid")?.Value;
                var sessions = context.HttpContext.RequestServices
                    .GetRequiredService<BffSessionStore>();

                if (string.IsNullOrWhiteSpace(sid) || !sessions.Exists(sid))
                {
                    context.RejectPrincipal();
                }

                return Task.CompletedTask;
            },
            OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/", () => Results.Ok(new
{
    service = "AuthDemo BFF",
    api = "http://localhost:5080",
    endpoints = new[] { "/bff/csrf", "/bff/login", "/bff/me", "/bff/admin", "/bff/logout" }
}));

app.MapGet("/bff/csrf", (HttpContext context, IAntiforgery antiforgery) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Ok(new
    {
        requestToken = tokens.RequestToken,
        headerName = "X-CSRF-TOKEN"
    });
});

app.MapPost("/bff/login", async Task<IResult> (
    HttpContext context,
    LoginRequest request,
    IAntiforgery antiforgery,
    IHttpClientFactory httpClientFactory,
    BffSessionStore sessions) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest(new { error = "Invalid or missing CSRF token." });
    }

    var api = httpClientFactory.CreateClient("api");
    using var response = await api.PostAsJsonAsync(
        "/token",
        new TokenRequest(request.Username, request.Password),
        context.RequestAborted);

    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
    {
        return Results.Unauthorized();
    }

    if (!response.IsSuccessStatusCode)
    {
        return Results.Problem("Authentication service returned an error.");
    }

    var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(
        cancellationToken: context.RequestAborted);

    if (tokens is null)
    {
        return Results.Problem("Authentication service returned an invalid response.");
    }

    var sid = Guid.NewGuid().ToString("N");
    sessions.Save(
        sid,
        new BffSession(
            tokens.AccessToken,
            tokens.RefreshToken,
            DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn)));

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, request.Username),
        new(ClaimTypes.NameIdentifier, request.Username),
        new("sid", sid)
    };

    var principal = new ClaimsPrincipal(
        new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

    await context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = request.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(request.RememberMe ? 24 * 7 : 8),
            AllowRefresh = true
        });

    return Results.Ok(new { user = request.Username, message = "BFF session created." });
});

app.MapGet("/bff/me", async Task<IResult> (
    HttpContext context,
    IHttpClientFactory httpClientFactory,
    BffTokenService tokenService) =>
{
    var sid = context.User.FindFirst("sid")?.Value;
    if (string.IsNullOrWhiteSpace(sid))
    {
        return Results.Unauthorized();
    }

    var accessToken = await tokenService.GetAccessTokenAsync(
        sid,
        context.RequestAborted);

    if (accessToken is null)
    {
        return Results.Unauthorized();
    }

    var api = httpClientFactory.CreateClient("api");
    using var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    using var response = await api.SendAsync(request, context.RequestAborted);
    var body = await response.Content.ReadAsStringAsync(context.RequestAborted);

    return Results.Content(
        body,
        "application/json",
        statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapGet("/bff/admin", async Task<IResult> (
    HttpContext context,
    IHttpClientFactory httpClientFactory,
    BffTokenService tokenService) =>
{
    var sid = context.User.FindFirst("sid")?.Value;
    if (string.IsNullOrWhiteSpace(sid))
    {
        return Results.Unauthorized();
    }

    var accessToken = await tokenService.GetAccessTokenAsync(
        sid,
        context.RequestAborted);

    if (accessToken is null)
    {
        return Results.Unauthorized();
    }

    var api = httpClientFactory.CreateClient("api");
    using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    using var response = await api.SendAsync(request, context.RequestAborted);
    var body = await response.Content.ReadAsStringAsync(context.RequestAborted);

    return Results.Content(
        body,
        "application/json",
        statusCode: (int)response.StatusCode);
}).RequireAuthorization();

app.MapPost("/bff/logout", async Task<IResult> (
    HttpContext context,
    IAntiforgery antiforgery,
    IHttpClientFactory httpClientFactory,
    BffSessionStore sessions) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest(new { error = "Invalid or missing CSRF token." });
    }

    var sid = context.User.FindFirst("sid")?.Value;

    if (!string.IsNullOrWhiteSpace(sid) &&
        sessions.TryGet(sid, out var session) &&
        session is not null)
    {
        try
        {
            var api = httpClientFactory.CreateClient("api");
            await api.PostAsJsonAsync(
                "/token/revoke",
                new RefreshRequest(session.RefreshToken),
                context.RequestAborted);
        }
        catch (HttpRequestException)
        {
            // Локальная BFF-сессия всё равно будет закрыта ниже.
        }

        sessions.Remove(sid);
    }

    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok(new { message = "BFF session closed." });
}).RequireAuthorization();

app.Run();

public sealed record LoginRequest(string Username, string Password, bool RememberMe = false);
public sealed record TokenRequest(string Username, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn);

public sealed record BffSession(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessExpiresAt);

public sealed class BffSessionStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, BffSession> _sessions = new();

    public void Save(string sid, BffSession session) => _sessions[sid] = session;

    public bool Exists(string sid) => _sessions.ContainsKey(sid);

    public bool TryGet(string sid, out BffSession? session) =>
        _sessions.TryGetValue(sid, out session);

    public void Remove(string sid) => _sessions.TryRemove(sid, out _);
}

public sealed class BffTokenService(
    IHttpClientFactory httpClientFactory,
    BffSessionStore sessions)
{
    public async Task<string?> GetAccessTokenAsync(
        string sid,
        CancellationToken cancellationToken)
    {
        if (!sessions.TryGet(sid, out var session) || session is null)
        {
            return null;
        }

        if (session.AccessExpiresAt > DateTimeOffset.UtcNow.AddSeconds(30))
        {
            return session.AccessToken;
        }

        var api = httpClientFactory.CreateClient("api");

        using var response = await api.PostAsJsonAsync(
            "/token/refresh",
            new RefreshRequest(session.RefreshToken),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            sessions.Remove(sid);
            return null;
        }

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(
            cancellationToken: cancellationToken);

        if (tokens is null)
        {
            sessions.Remove(sid);
            return null;
        }

        sessions.Save(
            sid,
            new BffSession(
                tokens.AccessToken,
                tokens.RefreshToken,
                DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn)));

        return tokens.AccessToken;
    }
}
