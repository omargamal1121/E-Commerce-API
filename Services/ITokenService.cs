namespace E_Commers.Services
{
	public interface ITokenService 
	{
		public Task<Result<string>> GenerateTokenAsync(string userId);


	}
}
