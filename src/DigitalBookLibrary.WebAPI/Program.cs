using System.Reflection;
using System.Text;
using System.Text.Json;
using DigitalBookLibrary.Application;
using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Application.Interfaces;
using DigitalBookLibrary.Infrastructure;
using DigitalBookLibrary.Infrastructure.Identity;
using DigitalBookLibrary.WebAPI.Middleware;
using DigitalBookLibrary.WebAPI.Services;
using DigitalBookLibrary.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

const string AuthRateLimitPolicy = "auth";
const string CorsPolicy = "frontend";

var builder = WebApplication.CreateBuilder(args);

// Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Current-user accessor (reads claims from HttpContext)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddControllers();

// JWT authentication
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // The [Authorize] attribute short-circuits before our exception middleware, so without these
        // handlers a 401/403 would return an empty body — breaking the "every response is an
        // ApiResponse of codes" contract the frontend relies on.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await WriteAuthFailureAsync(context.Response, StatusCodes.Status401Unauthorized, "UNAUTHORIZED");
            },
            OnForbidden = context =>
                WriteAuthFailureAsync(context.Response, StatusCodes.Status403Forbidden, "FORBIDDEN")
        };
    });

static async Task WriteAuthFailureAsync(HttpResponse response, int statusCode, string errorCode)
{
    response.StatusCode = statusCode;
    response.ContentType = "application/json";
    var body = ApiResponse.Fail(ResponseCodes.OperationFailed, errorCode);
    await response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
}

builder.Services.AddAuthorization();

// Rate limiting — auth endpoints are the ones worth brute-forcing, so they get a tighter budget.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter(AuthRateLimitPolicy, limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });

    // Anything over the limit still speaks our envelope.
    options.OnRejected = async (context, cancellationToken) =>
    {
        await WriteAuthFailureAsync(
            context.HttpContext.Response, StatusCodes.Status429TooManyRequests, "TOO_MANY_REQUESTS");
    };
});

// CORS — the React frontend's origins come from config; no blanket wildcard.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? Array.Empty<string>();
builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    }
}));

// Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Digital Book Library API",
        Version = "v1",
        Description =
            """
            A digital book library: catalogue, reading & downloading, ratings, threaded comments,
            saved lists, and an admin dashboard with an audit trail.

            **Response envelope** — every response is an `ApiResponse`:
            `{ "success": bool, "message": "CODE", "data": {...}, "errors": ["CODE", ...] }`.
            `message`/`errors` are stable CODES (not sentences); the client translates them
            (see docs/06-Error-Codes.md).

            **Authentication** — POST `/api/auth/login`, copy `data.accessToken`, then click
            **Authorize** and paste it. Writes and admin endpoints require the token; the catalogue
            reads are public. Seeded dev admin: `admin@digitalbooklibrary.local` / `Admin#12345`.
            """,
        Contact = new OpenApiContact { Name = "Digital Book Library", Url = new Uri("https://github.com/") }
    });

    // Surface the controllers' /// summaries (needs GenerateDocumentationFile in the csproj).
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter the JWT access token (without the 'Bearer ' prefix).",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", null), new List<string>() }
    });
});

var app = builder.Build();

// Apply migrations and seed the baseline data (roles + admin) on startup in Development so a
// fresh clone is runnable immediately. In production this should be an explicit deployment step.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
}

// Global exception handling must wrap the whole pipeline.
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
