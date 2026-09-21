using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Billing.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Billing.Api.Controllers;

public sealed record LoginRequest([Required] string Username, [Required] string Password);
public sealed record LoginResponse(string Token, string Username, string Role);

[ApiController, Route("api/auth")]
public sealed class AuthController(AuthenticationService authenticationService, SymmetricSecurityKey signingKey) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await authenticationService.ValidateCredentialsAsync(request.Username, request.Password, cancellationToken);
        if (user is null) return Unauthorized(new { message = "Invalid username or password." });

        var claims = new[] { new Claim(ClaimTypes.Name, user.Username), new Claim(ClaimTypes.Role, user.Role) };
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: credentials);
        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Username, user.Role));
    }

    [HttpPost("logout")]
    public IActionResult Logout() => NoContent();
}