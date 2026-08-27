using Cupo.Api.Auth;
using Cupo.Api.Data;
using Cupo.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cupo.Api.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher _hasher;
    private readonly JwtTokenGenerator _jwt;

    public AuthService(AppDbContext db, PasswordHasher hasher, JwtTokenGenerator jwt)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<User> RegisterAsync(string email, string password, CancellationToken ct)
    {
        email = email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new InvalidOperationException("EMAIL_TAKEN");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _hasher.Hash(password),
            Role = "User",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<(string Token, DateTimeOffset ExpiresAt)?> LoginAsync(
        string email, string password, CancellationToken ct)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || !_hasher.Verify(password, user.PasswordHash))
            return null;
        return _jwt.Create(user);
    }
}