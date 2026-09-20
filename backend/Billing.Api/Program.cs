using Billing.Application;
using Billing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddOpenApi();
builder.Services.AddDbContext<BillingDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("Billing") ?? "Data Source=billing.db"));
builder.Services.AddScoped<BillingCalculator>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    db.Database.EnsureCreated();
}
app.UseCors();
app.MapOpenApi();

app.MapControllers();

app.Run();
