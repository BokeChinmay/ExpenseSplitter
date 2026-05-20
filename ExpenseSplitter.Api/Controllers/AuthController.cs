using ExpenseSplitter.Api.Models;
using ExpenseSplitter.Api.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase {
    private readonly AuthService _auth;

    public AuthController(AuthService auth) { _auth = auth; }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request) {
        var result = await _auth.RegisterAsync(request);
        if (result == null) {
            return Conflict("An account with this email already exists.");
        }
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request) {
        var result = await _auth.LoginAsync(request);
        if (result == null) {
            return Unauthorized("Invalid email or password.");
        }
        return Ok(result);
    }
}