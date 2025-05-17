using E_Commers.Enums;
using E_Commers.Models;

namespace E_Commers.Services.AdminOpreationServices
{
	public interface IAdminOpreationServices
	{
		public Task<Result< bool>> AddAdminOpreationAsync(string description, Opreations opreation, string userid, int itemid);
		public Task<Result< bool>> DeleteAdminOpreationAsync(int id);
		public Task<Result< List<AdminOperationsLog>>> GetAllOpreationsAsync();
		public Task<Result<List<AdminOperationsLog>>> GetAllOpreationsByOpreationTypeAsync(Opreations opreation);
	}
}
