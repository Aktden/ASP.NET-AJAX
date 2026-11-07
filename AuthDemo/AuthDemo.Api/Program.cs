using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key is missing or too short. Set Jwt:Key (at least 32 bytes).");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AuthDemo";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AuthDemo.Api";

builder.Services.AddSingleton<RevokedSessionStore>();
builder.Services.AddSingleton<RevokedTokenStore>();
builder.Services.AddSingleton<RefreshTokenStore>();
builder.Services.AddSingleton<TokenService>();

var apiKeyDirectory = Path.Combine(builder.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(apiKeyDirectory);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(apiKeyDirectory))
    .SetApplicationName("AuthDemo.Api");

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = ".AuthDemo.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "Smart";
        options.DefaultChallengeScheme = "Smart";
    })
    .AddPolicyScheme("Smart", "Cookie or Bearer", options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Headers.Authorization.ToString()
                .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? JwtBearerDefaults.AuthenticationScheme
                : CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = ".AuthDemo.Cookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/cookie/login";
        options.LogoutPath = "/cookie/logout";

        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = context =>
            {
                var sid = context.Principal?.FindFirst("sid")?.Value;
                var sessions = context.HttpContext.RequestServices
                    .GetRequiredService<RevokedSessionStore>();

                if (string.IsNullOrWhiteSpace(sid) || sessions.IsRevoked(sid))
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
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var jti = context.Principal?
                    .FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                var revokedTokens = context.HttpContext.RequestServices
                    .GetRequiredService<RevokedTokenStore>();

                if (jti is not null && revokedTokens.IsRevoked(jti))
                {
                    context.Fail("Token revoked");
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CookieOnly", policy =>
    {
        policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy("CookieAdmin", policy =>
    {
        policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("admin");
    });

    options.AddPolicy("BearerOnly", policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy("BearerAdmin", policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("admin");
    });

    options.AddPolicy("Smart", policy =>
    {
        policy.AddAuthenticationSchemes("Smart");
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy("SmartAdmin", policy =>
    {
        policy.AddAuthenticationSchemes("Smart");
        policy.RequireAuthenticatedUser();
        policy.RequireRole("admin");
    });
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/", () => Results.Ok(new
{
    service = "AuthDemo API",
    authMethods = new[] { "Cookie", "JWT Bearer", "Combined Cookie/Bearer" },
    documentation = "See README.md"
}));

// -------------------- Cookie Authentication --------------------

app.MapGet("/cookie/csrf", (HttpContext context, IAntiforgery antiforgery) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Ok(new
    {
        requestToken = tokens.RequestToken,
        headerName = "X-CSRF-TOKEN"
    });
});

app.MapPost("/cookie/login", async Task<IResult> (
    HttpContext context,
    LoginRequest request,
    IAntiforgery antiforgery) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest(new { error = "Invalid or missing CSRF token." });
    }

    if (!DemoUsers.TryVerify(request.Username, request.Password, out var user))
    {
        return Results.Unauthorized();
    }

    var sid = Guid.NewGuid().ToString("N");
    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, user.Username),
        new(ClaimTypes.NameIdentifier, user.Username),
        new("sid", sid)
    };

    claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

    var identity = new ClaimsIdentity(
        claims,
        CookieAuthenticationDefaults.AuthenticationScheme);

    var principal = new ClaimsPrincipal(identity);

    await context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = request.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(request.RememberMe ? 24 * 7 : 8),
            AllowRefresh = true
        });

    return Results.Ok(new { user = user.Username, sid });
});

app.MapPost("/cookie/logout", async Task<IResult> (
    HttpContext context,
    IAntiforgery antiforgery,
    RevokedSessionStore revokedSessions) =>
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
    if (!string.IsNullOrWhiteSpace(sid))
    {
        revokedSessions.Revoke(sid);
    }

    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok(new { message = "Signed out." });
}).RequireAuthorization("CookieOnly");

app.MapGet("/cookie/me", (HttpContext context) =>
    Results.Ok(new
    {
        name = context.User.Identity?.Name,
        roles = context.User.FindAll(ClaimTypes.Role).Select(claim => claim.Value)
    }))
    .RequireAuthorization("CookieOnly");

app.MapGet("/cookie/admin", () => Results.Ok(new { message = "Cookie admin area." }))
    .RequireAuthorization("CookieAdmin");

// -------------------- JWT Bearer --------------------

app.MapPost("/token", IResult (
    TokenRequest request,
    TokenService tokenService,
    RefreshTokenStore refreshTokens) =>
{
    if (!DemoUsers.TryVerify(request.Username, request.Password, out var user))
    {
        return Results.Unauthorized();
    }

    var accessToken = tokenService.CreateAccessToken(
        user.Username,
        user.Roles,
        TimeSpan.FromMinutes(15));

    var refreshToken = tokenService.CreateRefreshToken();
    refreshTokens.Save(
        user.Username,
        refreshToken,
        DateTimeOffset.UtcNow.AddDays(7));

    return Results.Ok(new TokenResponse(
        accessToken,
        refreshToken,
        "Bearer",
        900));
});

app.MapPost("/token/refresh", IResult (
    RefreshRequest request,
    TokenService tokenService,
    RefreshTokenStore refreshTokens) =>
{
    if (!refreshTokens.TryConsume(request.RefreshToken, out var entry) ||
        entry is null ||
        entry.ExpiresAt <= DateTimeOffset.UtcNow)
    {
        return Results.Unauthorized();
    }

    if (!DemoUsers.TryGet(entry.Username, out var user))
    {
        return Results.Unauthorized();
    }

    var accessToken = tokenService.CreateAccessToken(
        user.Username,
        user.Roles,
        TimeSpan.FromMinutes(15));

    var newRefreshToken = tokenService.CreateRefreshToken();
    refreshTokens.Save(
        user.Username,
        newRefreshToken,
        DateTimeOffset.UtcNow.AddDays(7));

    return Results.Ok(new TokenResponse(
        accessToken,
        newRefreshToken,
        "Bearer",
        900));
});

app.MapPost("/token/revoke", (
    RefreshRequest request,
    RefreshTokenStore refreshTokens) =>
{
    refreshTokens.Revoke(request.RefreshToken);
    return Results.Ok(new { message = "Refresh token revoked." });
});

app.MapPost("/token/revoke-access", IResult (
    HttpContext context,
    RevokedTokenStore revokedTokens) =>
{
    var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
    if (string.IsNullOrWhiteSpace(jti))
    {
        return Results.Unauthorized();
    }

    revokedTokens.Revoke(jti);
    return Results.Ok(new { message = "Access token revoked." });
}).RequireAuthorization("BearerOnly");

app.MapGet("/api/me", (HttpContext context) =>
    Results.Ok(new
    {
        sub = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
        roles = context.User.FindAll("role").Select(claim => claim.Value)
    }))
    .RequireAuthorization("BearerOnly");

app.MapGet("/api/admin", () => Results.Ok(new { message = "JWT admin area." }))
    .RequireAuthorization("BearerAdmin");

// -------------------- Combined Cookie + Bearer --------------------

app.MapGet("/combined/me", (HttpContext context) =>
    Results.Ok(new
    {
        name = context.User.Identity?.Name
              ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
        authenticationType = context.User.Identity?.AuthenticationType,
        roles = context.User.FindAll(ClaimTypes.Role)
            .Concat(context.User.FindAll("role"))
            .Select(claim => claim.Value)
            .Distinct()
    }))
    .RequireAuthorization("Smart");

app.MapGet("/combined/admin", () =>
    Results.Ok(new { message = "Admin area: Cookie or Bearer accepted." }))
    .RequireAuthorization("SmartAdmin");

app.Run();

// -------------------- Models and in-memory demo stores --------------------

public sealed record LoginRequest(string Username, string Password, bool RememberMe = false);
public sealed record TokenRequest(string Username, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn);

public sealed record DemoUser(string Username, string PasswordHash, string[] Roles);
public sealed record RefreshTokenEntry(string Username, DateTimeOffset ExpiresAt);

public static class DemoUsers
{
    private static readonly Dictionary<string, DemoUser> Users =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["alice"] = new DemoUser(
                "alice",
                PasswordHashing.Hash("p@ss"),
                new[] { "user", "admin" }),
            ["bob"] = new DemoUser(
                "bob",
                PasswordHashing.Hash("p@ss"),
                new[] { "user" })
        };

    public static bool TryGet(string username, out DemoUser user) =>
        Users.TryGetValue(username, out user!);

    public static bool TryVerify(string username, string password, out DemoUser user)
    {
        if (Users.TryGetValue(username, out user!) &&
            PasswordHashing.Verify(password, user.PasswordHash))
        {
            return true;
        }

        user = null!;
        return false;
    }
}

public static class PasswordHashing
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(SaltSize);
        var hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            HashSize);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);
            var actual = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                System.Security.Cryptography.HashAlgorithmName.SHA256,
                expected.Length);

            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                actual,
                expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed class RevokedSessionStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _sessions = new();

    public bool IsRevoked(string sid) => _sessions.ContainsKey(sid);
    public void Revoke(string sid) => _sessions.TryAdd(sid, 0);
}

public sealed class RevokedTokenStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _tokens = new();

    public bool IsRevoked(string jti) => _tokens.ContainsKey(jti);
    public void Revoke(string jti) => _tokens.TryAdd(jti, 0);
}

public sealed class RefreshTokenStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, RefreshTokenEntry> _tokens = new();

    public void Save(string username, string token, DateTimeOffset expiresAt) =>
        _tokens[Hash(token)] = new RefreshTokenEntry(username, expiresAt);

    public bool TryConsume(string token, out RefreshTokenEntry? entry) =>
        _tokens.TryRemove(Hash(token), out entry);

    public void Revoke(string token) =>
        _tokens.TryRemove(Hash(token), out _);

    private static string Hash(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}

public sealed class TokenService(IConfiguration configuration)
{
    private readonly string _key = configuration["Jwt:Key"]!;
    private readonly string _issuer = configuration["Jwt:Issuer"] ?? "AuthDemo";
    private readonly string _audience = configuration["Jwt:Audience"] ?? "AuthDemo.Api";

    public string CreateAccessToken(
        string username,
        IEnumerable<string> roles,
        TimeSpan lifetime)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: now.Add(lifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateRefreshToken() =>
        Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
}
