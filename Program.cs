using ECommerceApi.Data;
using ECommerceApi.Models;
using ECommerceApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ✅ Load config
var configuration = builder.Configuration;

// ✅ Override with Environment Variables (for Render/Docker)
var defaultConnection = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");
if (!string.IsNullOrWhiteSpace(defaultConnection))
    configuration["ConnectionStrings:DefaultConnection"] = defaultConnection;

var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

if (!string.IsNullOrWhiteSpace(jwtKey)) configuration["Jwt:Key"] = jwtKey;
if (!string.IsNullOrWhiteSpace(jwtIssuer)) configuration["Jwt:Issuer"] = jwtIssuer;
if (!string.IsNullOrWhiteSpace(jwtAudience)) configuration["Jwt:Audience"] = jwtAudience;

// 🔐 JWT Configuration
var jwtSettings = configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"]!;

// 🌐 CORS Origins from appsettings.json or ENV
var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>();

// 📦 DB Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

// 👤 Identity Configuration
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 📦 Dependency Injection
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<RazorpayService>();
builder.Services.AddScoped<CloudinaryService>();

builder.Services.AddControllers();

// 🌍 CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowedOrigins!)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// 🔐 JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero,
        NameClaimType = ClaimTypes.Name
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// ✅ Middleware
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ✅ Migrate DB and Seed SuperAdmin
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    SeedData.SeedSuperAdmin(db, configuration);
}

app.Run();
