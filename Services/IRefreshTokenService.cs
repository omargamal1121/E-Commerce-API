namespace E_Commers.Services
{
	public interface IRefreshTokenService 
	{
	
		public Task<Result<bool>> ValidateRefreshTokenAsync(string userId, string Refreshtoken);
		public Task<Result<bool>> RemoveRefreshTokenAsync(string userId);
		public Task<Result<string>> GenerateRefreshToken(string userId);
		public Task<Result<string>> RefreshToken(string userId, string refreshToken);

	}
}
