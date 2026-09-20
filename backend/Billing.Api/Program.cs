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
builder.Services.AddDbContext<BillingDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("Billing") ?? "Data Source=billing.db"));
builder.Services.AddScoped<BillingCalculator>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
var signingKey = builder.Configuration["Authentication:SigningKey"] ?? "development-only-change-this-signing-key-2026";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1) };
});
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    db.Database.EnsureCreated();
    db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Users (Id INTEGER NOT NULL CONSTRAINT PK_Users PRIMARY KEY AUTOINCREMENT, Username TEXT NOT NULL, PasswordHash TEXT NOT NULL, Role TEXT NOT NULL, IsActive INTEGER NOT NULL, CreatedAt TEXT NOT NULL);");
    db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Username ON Users (Username);");
    if (!db.Users.Any())
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var admin = new User { Username = "admin", PasswordHash = "pending", Role = "Admin" };
        var staff = new User { Username = "staff", PasswordHash = "pending", Role = "BillingStaff" };
        admin.PasswordHash = hasher.HashPassword(admin, "admin123");
        staff.PasswordHash = hasher.HashPassword(staff, "staff123");
        db.Users.AddRange(admin, staff);
        db.SaveChanges();
    }
}
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();

app.MapControllers();

app.Run();
