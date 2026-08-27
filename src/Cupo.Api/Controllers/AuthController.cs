using Cupo.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cupo.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    public AuthController(AuthService auth) => _auth = auth;

    public record AuthRequest(string Email, string Password);

    [HttpPost("register")]
    public async Task<IActionResult> Register(AuthRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "Email y password requeridos" });
        try
        {
            var user = await _auth.RegisterAsync(req.Email, req.Password, ct);
            return Created("/api/auth/register", new { user.Id, user.Email, user.Role });
        }
        catch (InvalidOperationException ex) when (ex.Message == "EMAIL_TAKEN")
        {
            return Conflict(new { error = "Email ya registrado", code = "EMAIL_TAKEN" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(AuthRequest req, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(req.Email, req.Password, ct);
        if (result is null)
            return Unauthorized(new { error = "Credenciales inválidas", code = "UNAUTHORIZED" });
        return Ok(new { token = result.Value.Token, expiresAt = result.Value.ExpiresAt });
    }
}