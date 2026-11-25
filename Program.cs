using System.Text;
using AISmokingDetection.Business.Background;
using AISmokingDetection.Business.Interfaces;
using AISmokingDetection.Business.Services;
using AISmokingDetection.Common;
using AISmokingDetection.Data;
using AISmokingDetection.Options;
using AISmokingDetection.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// ---- Options ----
builder.Services.Configure<AiServiceOptions>(
    builder.Configuration.GetSection(AiServiceOptions.SectionName));
builder.Services.Configure<EvidenceOptions>(
    builder.Configuration.GetSection(EvidenceOptions.SectionName));

// ---- Services ----
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IProcessingJobService, ProcessingJobService>();
builder.Services.AddScoped<IEvidenceService, EvidenceService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IAiCallbackService, AiCallbackService>();
builder.Services.AddSingleton<IEvidenceStorage, LocalFileEvidenceStorage>();

// Backend B -> AI submission client
builder.Services.AddHttpClient<IAiJobSubmitter, AiJobSubmitter>((sp, client) =>
{
    var ai = sp.GetRequiredService<IOptions<AiServiceOptions>>().Value;

    if (!string.IsNullOrWhiteSpace(ai.BaseUrl))
        client.BaseAddress = new Uri(ai.BaseUrl);

    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, ai.TimeoutSeconds));

    if (!string.IsNullOrWhiteSpace(ai.OutboundApiKey))
        client.DefaultRequestHeaders.Add(AuthConstants.ApiKeyHeader, ai.OutboundApiKey);
});

builder.Services.AddHostedService<JobMonitorService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AIsmokingCorsPolicy",
        Policy =>
        {
            Policy.
            WithOrigins("https://localhost:7288", "http://localhost:5129").
            AllowAnyHeader().
            AllowAnyMethod();
        });
});

// ---- Staff authentication (JWT) ----
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "JWT configuration is missing. Check User Secrets.");
}

// ---- AI service authentication (API key) ----
var inboundKeys = builder.Configuration
    .GetSection($"{AiServiceOptions.SectionName}:InboundApiKeys")
    .Get<string[]>() ?? Array.Empty<string>();

if (!inboundKeys.Any(k => !string.IsNullOrWhiteSpace(k)))
{
    throw new InvalidOperationException(
        "AiService:InboundApiKeys is missing. Set it with User Secrets / environment variables.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Convert.FromBase64String(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        AuthConstants.ApiKeyScheme, _ => { });

builder.Services.AddAuthorization(options =>
{
    // Only the AI service (valid API key) may call the callback endpoints.
    options.AddPolicy(AuthConstants.AiServicePolicy, policy =>
    {
        policy.AddAuthenticationSchemes(AuthConstants.ApiKeyScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole(AuthConstants.AiServiceRole);
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AIsmokingCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
