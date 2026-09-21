using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Billing.UnitTests;

public sealed class ApiIntegrationTests : IAsyncLifetime
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"billing-tests-{Guid.NewGuid():N}.db");
    private WebApplicationFactory<Program>? factory;

    public Task InitializeAsync()
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Billing"] = $"Data Source={databasePath}",
                ["Authentication:SigningKey"] = "integration-test-signing-key-at-least-32-bytes",
                ["Authentication:BootstrapUsersEnabled"] = "true"
            }));
        });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (factory is not null) await factory.DisposeAsync();
        File.Delete(databasePath);
    }

    [Fact]
    public async Task Health_endpoint_reports_database_ready()
    {
        using var client = factory!.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_rejects_anonymous_requests()
    {
        using var client = factory!.CreateClient();

        var response = await client.GetAsync("/api/menu");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_load_menu_categories()
    {
        using var client = factory!.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "admin123" });
        loginResponse.EnsureSuccessStatusCode();
        using var login = await JsonDocument.ParseAsync(await loginResponse.Content.ReadAsStreamAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.RootElement.GetProperty("token").GetString());

        var categories = await client.GetFromJsonAsync<MenuCategory[]>("/api/menu/categories");

        Assert.Contains(categories!, category => category.Name == "Snacks");
    }

    [Fact]
    public async Task Authenticated_staff_can_create_concurrent_orders_with_unique_numbers()
    {
        using var client = factory!.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { username = "staff", password = "staff123" });
        loginResponse.EnsureSuccessStatusCode();
        using var login = await JsonDocument.ParseAsync(await loginResponse.Content.ReadAsStreamAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.RootElement.GetProperty("token").GetString());

        var requests = Enumerable.Range(1, 4).Select(index => client.PostAsJsonAsync("/api/orders", new
        {
            items = new[] { new { menuItemId = 1, quantity = 1 } },
            discount = 0,
            paymentMethod = "Cash",
            customerName = $"Customer {index}"
        }));
        var responses = await Task.WhenAll(requests);
        foreach (var response in responses)
        {
            Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {response.Headers.WwwAuthenticate} {await response.Content.ReadAsStringAsync()}");
        }

        var orderNumbers = await Task.WhenAll(responses.Select(async response =>
        {
            using var order = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return order.RootElement.GetProperty("orderNumber").GetString();
        }));
        Assert.Equal(orderNumbers.Length, orderNumbers.Distinct().Count());
    }

    private sealed record MenuCategory(int Id, string Name);
}
