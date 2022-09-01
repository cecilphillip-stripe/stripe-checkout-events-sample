using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StripeEventsCheckout.WebHost.Models;

namespace StripeEventsCheckout.WebHost.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class SecurityController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecurityController> _logger;

    public SecurityController(IConfiguration configuration,  ILogger<SecurityController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    
    [HttpPost("AuthenticateJwt")]
    public async Task<ActionResult> AuthenticateJwt(AuthenticationRequest authRequest)
    {
        var userInfo = await ValidateUser(authRequest);
        if (userInfo == null)
            return BadRequest();
        
        var token = CreateJwtToken(userInfo);
        
        return Ok(new{authenticated = true, token});
    }

    private string CreateJwtToken(UserInfo userInfo)
    {
        var jwtSettings = _configuration.GetSection("JWTSettings");
        var securityKeyStr = jwtSettings.GetValue<string>("SigningKey");
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKeyStr));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
 
        var claims = new List<Claim>(){
            new(JwtRegisteredClaimNames.Email, userInfo.Email),
            new(JwtRegisteredClaimNames.Sub , userInfo.UserName),
            new(JwtRegisteredClaimNames.Iat , DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        
        var token = new JwtSecurityToken(
            issuer: jwtSettings.GetValue<string>("Issuer"),
            audience: jwtSettings.GetValue<string>("Audience"),
            expires: DateTime.Now.AddDays(1),
            
            signingCredentials: credentials,
            claims: claims
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<UserInfo> ValidateUser(AuthenticationRequest authRequest)
    {
        await Task.CompletedTask;
        return new(Guid.NewGuid().ToString("N"),"cecilphillip", "cecilphillip@email.com");
    }
}