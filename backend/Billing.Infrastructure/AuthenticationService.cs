using Billing.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure;

public sealed class AuthenticationService(BillingDbContext db, IPasswordHasher<User> passwordHasher)
{
    public async Task<User?> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Username == username && candidate.IsActive, cancellationToken);
        if (user is null) return null;
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }
}