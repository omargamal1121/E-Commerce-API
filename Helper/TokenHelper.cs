using E_Commers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace E_Commers.Helper
{
	public class TokenHelper
	{
		private readonly IConnectionMultiplexer _redis;
		private readonly ILogger<TokenHelper> _logger;
		private readonly IConfiguration _config;
		private readonly UserManager<Customer> _userManager;
		private readonly IDatabase _database;

		public TokenHelper(ILogger<TokenHelper> logger, IConnectionMultiplexer redis, IConfiguration config, UserManager<Customer> userManager)
		{
			_logger = logger;
			_userManager = userManager;
			_redis = redis;
			_database = _redis.GetDatabase();
			_config = config;
		}


		public async Task<ResultDto<string>> RefreshToken(string userId, string refreshToken)
		{
			_logger.LogInformation("🔄 RefreshToken() started for User ID: {UserId}", userId);

			Customer? user = await _userManager.FindByIdAsync(userId);
			if (user is null)
			{
				_logger.LogWarning("❌ Invalid User ID: {UserId}", userId);
				return ResultDto<string>.Fail("Invalid User ID: {UserId}");
			}

		
			string? storedRefreshToken = await _database.StringGetAsync($"RefreshToken:{userId}");

			if (string.IsNullOrEmpty(storedRefreshToken) || !storedRefreshToken.Equals(refreshToken))
			{
				_logger.LogWarning("⚠️ Invalid Refresh Token for User ID: {UserId}", userId);
				return ResultDto<string>.Fail($"⚠️ Invalid Refresh Token for User ID: {userId}");
			}

			return await GenerateTokenAsync(userId);
		}


		public async Task<ResultDto<string>> GenerateRefreshToken(string userId)
		{
			_logger.LogInformation("🔑 Generating Refresh Token for User ID: {UserId}", userId);

			if (await _userManager.FindByIdAsync(userId) is null)
			{
				_logger.LogWarning("❌ Invalid User ID: {UserId}", userId);
				return ResultDto<string>.Fail("Invalid User ID: {UserId}");
			}

		
			string token = Guid.NewGuid().ToString();
			await _database.StringSetAsync($"RefreshToken:{userId}", token, expiry: TimeSpan.FromDays(1));
			_logger.LogInformation("RefreshToken Generated");
			return ResultDto<string>.Ok(token, "RefreshToken Generated");
		}


	
		public async Task<ResultDto<bool>> RemoveRefreshTokenAsync(string userId)
		{
			_logger.LogInformation("🗑 Removing Refresh Token for User ID: {UserId}", userId);

			Customer? customer = await _userManager.FindByIdAsync(userId);
			if (customer is null)
			{
				_logger.LogWarning("❌ Invalid User ID: {UserId}", userId);
				return ResultDto<bool>.Fail($"❌ Invalid User ID: {userId}");
			}


			IdentityResult result = await _userManager.UpdateSecurityStampAsync(customer);
			if (!result.Succeeded)
			{
				_logger.LogWarning("⚠️ Failed to update SecurityStamp for User {UserId}", userId);
				return ResultDto<bool>.Fail($"❌ Failed to update SecurityStamp for User: {userId}");
			}

			bool deleted = await _database.KeyDeleteAsync($"RefreshToken:{userId}");
			if (!deleted)
			{
				_logger.LogWarning("⚠️ Failed to remove RefreshToken for User {UserId}", userId);
				return ResultDto<bool>.Fail($"❌ Failed to remove RefreshToken for User {userId}");
			}

			return ResultDto<bool>.Ok(true,$"RefreshToken Removed");
		}

		public async Task<ResultDto<string>>GenerateTokenAsync(string userId)
		{
			_logger.LogInformation("🔐 Generating Access Token for User ID: {UserId}", userId);

			Customer? user = await _userManager.FindByIdAsync(userId);
			if (user is null)
			{
				_logger.LogWarning("❌ Invalid User ID: {UserId}", userId);
				return ResultDto<string>.Fail("Invalid User ID: {UserId}");
			}

			string secretKey = _config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key is missing in appsettings.json");
			string issuer = _config["Jwt:Issure"] ?? "DefaultIssuer";
			string audience = _config["Jwt:Audience"] ?? "DefaultAudience";

			List<Claim> claims = new List<Claim>()
			{
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
			_logger.LogInformation("✅ Access Token generated successfully for User ID: {UserId}", userId);
			return  ResultDto<string>.Ok(tokenString,$"✅ Access Token generated successfully for User ID: {userId}") ;
		}
		public async Task<ResultDto<bool>> ValidateRefreshTokenAsync(string userId,string Refreshtoken)
		{
			_logger.LogInformation($"In {nameof(ValidateRefreshTokenAsync)} Method");
			string? storedtoken= await	_database.StringGetAsync($"RefreshToken:{userId}");
			if (string.IsNullOrEmpty(storedtoken)||!storedtoken.Equals(Refreshtoken,StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogWarning("Refreshtoken Invalid Or Doesn't Exsist");
				return ResultDto<bool>.Fail("Refreshtoken Invalid Or Doesn't Exsist");
			}
			_logger.LogInformation("Valid Refreshtoken");

			return ResultDto<bool>.Ok(true, "Valid Refreshtoken");
		}
	}
}
