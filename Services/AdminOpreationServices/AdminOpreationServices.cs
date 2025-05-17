using E_Commers.Enums;
using E_Commers.Models;
using E_Commers.Services.Category;
using E_Commers.UOW;

namespace E_Commers.Services.AdminOpreationServices
{
	public class AdminOpreationServices : IAdminOpreationServices
	{
		private readonly ILogger<AdminOpreationServices> _logger;
		private readonly IUnitOfWork _unitOfWork;
		public AdminOpreationServices(IUnitOfWork unitOfWork,ILogger<AdminOpreationServices> logger)
		{
			_logger = logger;
			_unitOfWork = unitOfWork;
		}
		public async Task<Result<bool>> AddAdminOpreationAsync(string description, Opreations opreation, string userid, int itemid)
		{
			_logger.LogInformation($"Execute {nameof(AddAdminOpreationAsync)}");
			var adminopreation = new AdminOperationsLog
			{
				Description = description,
				AdminId = userid,
				ItemId = itemid,
				OperationType = opreation,


			};
			var iscreated = await _unitOfWork.Repository<AdminOperationsLog>().CreateAsync(adminopreation);
			if (!iscreated.Success)
			{
				_logger.LogError(iscreated.Message);
				return Result<bool>.Fail(iscreated.Message);
			}
			return Result<bool>.Ok(true);
		}

		public Task<Result<bool>> DeleteAdminOpreationAsync(int id)
		{
			throw new NotImplementedException();
		}

		public Task<Result<List<AdminOperationsLog>>> GetAllOpreationsAsync()
		{
			throw new NotImplementedException();
		}

		public Task<Result<List<AdminOperationsLog>>> GetAllOpreationsByOpreationTypeAsync(Opreations opreation)
		{
			throw new NotImplementedException();
		}
	}
}
