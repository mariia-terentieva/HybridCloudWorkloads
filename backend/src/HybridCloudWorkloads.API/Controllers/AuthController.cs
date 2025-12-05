using HybridCloudWorkloads.API.Models;
using HybridCloudWorkloads.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace HybridCloudWorkloads.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest("Passwords do not match");
        }

        var userExists = await _userManager.FindByEmailAsync(request.Email);
        if (userExists != null)
        {
            return BadRequest("User with this email already exists");
        }

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { Message = "User created successfully" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized("Invalid credentials");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
        {
            return Unauthorized("Invalid credentials");
        }

        var token = GenerateJwtToken(user);
        
        return Ok(new LoginResponse
        {
            Token = token,
            Expiration = DateTime.UtcNow.AddDays(7),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName
            }
        });
    }

    [Authorize]
[HttpPut("profile")]
public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
{
    var userId = GetCurrentUserId();
    var user = await _userManager.FindByIdAsync(userId.ToString());
    
    if (user == null)
        return NotFound("User not found");

    user.FirstName = request.FirstName;
    user.LastName = request.LastName;
    user.UpdatedAt = DateTime.UtcNow;

    var result = await _userManager.UpdateAsync(user);
    
    if (!result.Succeeded)
    {
        return BadRequest(result.Errors);
    }

    return Ok(new 
    { 
        Message = "Profile updated successfully",
        User = new 
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName
        }
    });
}

[Authorize]
[HttpPut("change-password")]
public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
{
    // Проверка совпадения паролей
    if (request.NewPassword != request.ConfirmNewPassword)
    {
        return BadRequest("New password and confirmation do not match");
    }

    var userId = GetCurrentUserId();
    var user = await _userManager.FindByIdAsync(userId.ToString());
    
    if (user == null)
        return NotFound("User not found");

    // Дополнительная валидация пароля
    var passwordValidator = new PasswordValidator<User>();
    var validationResult = await passwordValidator.ValidateAsync(_userManager, user, request.NewPassword);
    
    if (!validationResult.Succeeded)
    {
        return BadRequest(validationResult.Errors);
    }

    var result = await _userManager.ChangePasswordAsync(
        user, 
        request.CurrentPassword, 
        request.NewPassword
    );
    
    if (!result.Succeeded)
    {
        return BadRequest(result.Errors);
    }

    return Ok(new { Message = "Password changed successfully" });
}

private Guid GetCurrentUserId()
{
    return Guid.Parse(_userManager.GetUserId(User)!);
}

public class UpdateProfileRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}