using E_Commers.Models;
using E_Commers.Services;

namespace E_Commers.Interfaces
{
	public interface ITokenService 
	{
		public Task<Result<string>> GenerateTokenAsync(Customer user);


	}
}
