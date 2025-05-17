using E_Commers.Interfaces;
using E_Commers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace E_Commers.Services
{
	public class TokenService:ITokenService
	{
		private readonly ILogger<TokenService> _logger;
		private readonly IConfiguration _config;
		private readonly UserManager<Customer> _userManager;


		public TokenService(ILogger<TokenService> logger,  IConfiguration config, UserManager<Customer> userManager)
		{
			_logger = logger;
			_userManager = userManager;
			_config = config;
		}

		public async Task<Result<string>>GenerateTokenAsync(string userId)
		{
			_logger.LogInformation("🔐 Generating Access Token for User ID: {UserId}", userId);

			Customer? user = await _userManager.FindByIdAsync(userId);
			if (user is null)
			{
				_logger.LogWarning($"❌ Invalid User ID: {userId}");
				return Result<string>.Fail($"Invalid User ID: {userId}");
			}

			string secretKey = _config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key is missing in appsettings.json");
			string issuer = _config["Jwt:Issuer"] ?? "DefaultIssuer";
			string audience = _config["Jwt:Audience"] ?? "DefaultAudience";

			List<Claim> claims = new List<Claim>()
			{
				new(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
				new(ClaimTypes.NameIdentifier, user.Id),
				new(ClaimTypes.Name, user.Name ?? user.UserName ?? string.Empty),
				new("UserName", user.UserName ?? string.Empty),
				new("SecurityStamp", user.SecurityStamp ?? string.Empty),
				new(ClaimTypes.Email, user.Email ?? string.Empty),
				new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
			};
			foreach(var role in await _userManager.GetRolesAsync(user))
			{
				claims.Add( new Claim ( ClaimTypes.Role, role));
			}

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
			var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			if (!double.TryParse(_config["Jwt:ExpiresInMinutes"], out double expiresInMinutes))
			{
				_logger.LogWarning("⚠️ JWT ExpiresInMinutes is missing, using default (10 minutes).");
				expiresInMinutes = 10;
			}

			JwtSecurityToken token = new JwtSecurityToken(
				issuer: issuer,
				audience: audience,
				expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
				claims: claims,
				signingCredentials: signingCredentials
			);

			string tokenString = new JwtSecurityTokenHandler().WriteToken(token);
			_logger.LogInformation($"✅ Access Token generated successfully for User ID: {userId}");
			return  Result<string>.Ok(tokenString,$"✅ Access Token generated successfully for User ID: {userId}") ;
		}
	
	}
}
