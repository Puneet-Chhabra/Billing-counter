using Billing.Application;
using Billing.Infrastructure;
using Billing.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddOpenApi();
var databaseProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("Billing") ?? throw new InvalidOperationException("ConnectionStrings:Billing is not configured.");
builder.Services.AddDbContext<BillingDbContext>(options =>
{
    if (string.Equals(databaseProvider, "SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connectionString, sql => sql
            .MigrationsAssembly("Billing.Migrations.SqlServer")
            .EnableRetryOnFailure(12, TimeSpan.FromSeconds(10), null));
    }
    else if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        throw new InvalidOperationException($"Unsupported database provider '{databaseProvider}'.");
    }
});
builder.Services.AddScoped<BillingCalculator>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddHealthChecks().AddDbContextCheck<BillingDbContext>();
var signingKey = builder.Configuration["Authentication:SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
{
    throw new InvalidOperationException("Authentication:SigningKey must be configured with at least 32 bytes.");
}
var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)) { KeyId = "billing-api" };
builder.Services.AddSingleton(securityKey);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = securityKey, ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1) };
});
builder.Services.AddAuthorization();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length > 0) policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    db.Database.Migrate();
    if (builder.Configuration.GetValue<bool>("Authentication:BootstrapUsersEnabled") && !db.Users.Any())
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        foreach (var userConfiguration in builder.Configuration.GetSection("Authentication:BootstrapUsers").GetChildren())
        {
            var username = userConfiguration["Username"] ?? throw new InvalidOperationException("A bootstrap username is missing.");
            var password = userConfiguration["Password"] ?? throw new InvalidOperationException($"A bootstrap password is missing for '{username}'.");
            var role = userConfiguration["Role"] ?? throw new InvalidOperationException($"A bootstrap role is missing for '{username}'.");
            var user = new User { Username = username, PasswordHash = "pending", Role = role };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);
        }
        db.SaveChanges();
    }
}
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
