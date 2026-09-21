using Billing.Domain;
using Billing.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Billing.Api.Controllers;

public sealed record MenuCategoryResponse(int Id, string Name);

[ApiController, Route("api/menu")]
[Authorize(Roles = "Admin,BillingStaff")]
public sealed class MenuController(BillingDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<MenuItem>> Get(CancellationToken cancellationToken) => await db.MenuItems.Include(item => item.Category).Where(item => item.IsAvailable).OrderBy(item => item.Category!.Name).ThenBy(item => item.Name).ToListAsync(cancellationToken);

    [Authorize(Roles = "Admin"), HttpGet("manage")]
    public async Task<IReadOnlyList<MenuItem>> Manage(CancellationToken cancellationToken) => await db.MenuItems.Include(item => item.Category).OrderBy(item => item.Category!.Name).ThenBy(item => item.Name).ToListAsync(cancellationToken);

    [Authorize(Roles = "Admin"), HttpGet("categories")]
    public async Task<IReadOnlyList<MenuCategoryResponse>> Categories(CancellationToken cancellationToken) => await db.Categories.AsNoTracking().Where(category => category.IsActive).OrderBy(category => category.Name).Select(category => new MenuCategoryResponse(category.Id, category.Name)).ToListAsync(cancellationToken);

    [Authorize(Roles = "Admin"), HttpPost]
    public async Task<ActionResult<MenuItem>> Create(MenuItem item, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(item.Name) || item.Price < 0) return BadRequest("Name and price are invalid.");
        if (!await db.Categories.AnyAsync(category => category.Id == item.CategoryId && category.IsActive, cancellationToken)) return BadRequest("Category does not exist.");
        item.Id = 0; item.CreatedAt = DateTime.UtcNow; item.UpdatedAt = DateTime.UtcNow;
        db.MenuItems.Add(item); await db.SaveChangesAsync(cancellationToken); return Created($"api/menu/{item.Id}", item);
    }

    [Authorize(Roles = "Admin"), HttpPut("{id:int}")]
    public async Task<ActionResult<MenuItem>> Update(int id, MenuItem input, CancellationToken cancellationToken)
    {
        var item = await db.MenuItems.FindAsync([id], cancellationToken);
        if (item is null) return NotFound();
        if (string.IsNullOrWhiteSpace(input.Name) || input.Price < 0) return BadRequest("Name and price are invalid.");
        if (!await db.Categories.AnyAsync(category => category.Id == input.CategoryId && category.IsActive, cancellationToken)) return BadRequest("Category does not exist.");
        item.Name = input.Name; item.Description = input.Description; item.CategoryId = input.CategoryId; item.Price = input.Price; item.IsVegetarian = input.IsVegetarian; item.IsAvailable = input.IsAvailable; item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken); return item;
    }
}
