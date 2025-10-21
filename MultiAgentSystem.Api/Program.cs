using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MultiAgentSystem.Api.Agents;
using MultiAgentSystem.Api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ThisIsASecretKeyForDevelopmentPurposesOnly123!";
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

// Register HttpClient with optimized configuration
builder.Services.AddHttpClient<IUserAgent, UserAgent>(client =>
{
    client.BaseAddress = new Uri("https://localhost:58550");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IBingCustomSearchAgent, BingCustomSearchAgent>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register services
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IAISearchAgent, AISearchAgent>();
builder.Services.AddSingleton<MultiAgentSystem.Api.Services.IAIFoundryConnectionPool, MultiAgentSystem.Api.Services.AIFoundryConnectionPool>();
builder.Services.AddScoped<IUserAgent, UserAgent>();
builder.Services.AddScoped<IBingCustomSearchAgent, BingCustomSearchAgent>();
builder.Services.AddScoped<IOrchestratorAgent, OrchestratorAgent>();

// Register background services
builder.Services.AddHostedService<MultiAgentSystem.Api.Services.AgentPreloadService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
