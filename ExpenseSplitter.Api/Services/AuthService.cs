using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseSplitter.Api.Services;

public class AuthService {
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config) {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request) {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return null; //Email already exists
        
        var user = new User {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new AuthResponse(GenerateToken(user), user.Id, user.Name, user.Email);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request) {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null; //Invalid credentials

        return new AuthResponse(GenerateToken(user), user.Id, user.Name, user.Email);
    }

    private string GenerateToken(User user) {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));

        var token = new JwtSecurityToken(
            claims: new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name)
            },
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}