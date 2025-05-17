using E_Commers.DtoModels;
using E_Commers.Models;
using E_Commers.UOW;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace E_Commers.Controllers
{
		[Route("api/[Controller]")]
		[ApiController]
	//[Authorize(Roles ="Admin")]
	
	public class AdminOperationController : ControllerBase
	{
		private readonly IUnitOfWork _unitOfWork;
		private ILogger<AdminOperationController> _Logger;
		public AdminOperationController(ILogger<AdminOperationController> Logger, IUnitOfWork unitOfWork)
		{
			_Logger = Logger;
			_unitOfWork = unitOfWork;
			
		}

		[HttpGet]
		public async Task<ActionResult<ResponseDto>> GetAllOperation()
		{
			_Logger.LogInformation($"Execute:{nameof(GetAllOperation)}");
			var opreations = (await _unitOfWork.Repository<AdminOperationsLog>().GetAllAsync());
			if (!opreations.Success || opreations.Data is null)
				return NotFound(new ResponseDto { Message = opreations.Message });

			var list = opreations.Data.Select(x => new
			{
				x.Id,
				x.AdminId,
				x.ItemId,
				x.Description,
				x.CreatedAt
			});
			return Ok(new ResponseDto { Data=list});	
		}
	}
}
