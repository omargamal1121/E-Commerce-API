
using E_Commers.Models;
using Microsoft.AspNetCore.Identity;
using StackExchange.Redis;

namespace E_Commers.Services
{
	public class RefreshTokenService : IRefreshTokenService
	{
		private readonly IConnectionMultiplexer _redis;
		private readonly ILogger<TokenService> _logger;
		private readonly IConfiguration _config;
		private readonly UserManager<Customer> _userManager;
		private readonly IDatabase _database;
		private readonly ITokenService _tokenHelper;

		public RefreshTokenService(ITokenService tokenHelper, ILogger<TokenService> logger, IConnectionMultiplexer redis, IConfiguration config, UserManager<Customer> userManager)
		{
			_tokenHelper = tokenHelper;
			_logger = logger;
			_userManager = userManager;
			_redis = redis;
			_database = _redis.GetDatabase();
			_config = config;
		}

		public async Task<Result<string>> RefreshToken(string userId, string refreshToken)
		{
			_logger.LogInformation("🔄 RefreshToken() started for User ID: {UserId}", userId);

			Customer? user = await _userManager.FindByIdAsync(userId);
			if (user is null)
			{
				_logger.LogWarning("❌ Invalid User ID: {UserId}", userId);
				return Result<string>.Fail("Invalid User ID: {UserId}");
			}


			string? storedRefreshToken = await _database.StringGetAsync($"RefreshToken:{userId}");

			if (string.IsNullOrEmpty(storedRefreshToken) || !storedRefreshToken.Equals(refreshToken))
			{
				_logger.LogWarning("⚠️ Invalid Refresh Token for User ID: {UserId}", userId);
				return Result<string>.Fail($"⚠️ Invalid Refresh Token for User ID: {userId}");
			}

			return await _tokenHelper.GenerateTokenAsync(userId);
		}


		public async Task<Result<string>> GenerateRefreshToken(string userId)
		{
			_logger.LogInformation("🔑 Generating Refresh Token for User ID: {UserId}", userId);

			if (await _userManager.FindByIdAsync(userId) is null)
			{
				_logger.LogWarning("❌ Invalid User ID: {UserId}", userId);
				return Result<string>.Fail("Invalid User ID: {UserId}");
			}


			string token = Guid.NewGuid().ToString();
			await _database.StringSetAsync($"RefreshToken:{userId}", token, expiry: TimeSpan.FromDays(1));
			_logger.LogInformation("RefreshToken Generated");
			return Result<string>.Ok(token, "RefreshToken Generated");
		}

		public async Task<Result<bool>> RemoveRefreshTokenAsync(string userId)
		{
			_logger.LogInformation("🗑 Removing Refresh Token for User ID: {UserId}", userId);

			Customer? customer = await _userManager.FindByIdAsync(userId);
			if (customer is null)
			{
				_logger.LogWarning("❌ Invalid User ID: {UserId}", userId);
				return Result<bool>.Fail($"❌ Invalid User ID: {userId}");
			}


			IdentityResult result = await _userManager.UpdateSecurityStampAsync(customer);
			if (!result.Succeeded)
			{
				_logger.LogWarning("⚠️ Failed to update SecurityStamp for User {UserId}", userId);
				return Result<bool>.Fail($"❌ Failed to update SecurityStamp for User: {userId}");
			}

			bool deleted = await _database.KeyDeleteAsync($"RefreshToken:{userId}");
			if (!deleted)
			{
				_logger.LogWarning("⚠️ Failed to remove RefreshToken for User {UserId}", userId);
				return Result<bool>.Fail($"❌ Failed to remove RefreshToken for User {userId}");
			}

			return Result<bool>.Ok(true, $"RefreshToken Removed");
		}

		public async Task<Result<bool>> ValidateRefreshTokenAsync(string userId, string Refreshtoken)
		{
			_logger.LogInformation($"In {nameof(ValidateRefreshTokenAsync)} Method");
			string? storedtoken = await _database.StringGetAsync($"RefreshToken:{userId}");
			if (string.IsNullOrEmpty(storedtoken) || !storedtoken.Equals(Refreshtoken, StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogWarning("Refreshtoken Invalid Or Doesn't Exsist");
				return Result<bool>.Fail("Refreshtoken Invalid Or Doesn't Exsist");
			}
			_logger.LogInformation("Valid Refreshtoken");

			return Result<bool>.Ok(true, "Valid Refreshtoken");
		}
	}
}
