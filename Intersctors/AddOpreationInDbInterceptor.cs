using E_Commers.Enums;
using E_Commers.Models;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace E_Commers.Intersctors
{
	public class AddOpreationInDbInterceptor : SaveChangesInterceptor
	{
		private readonly IHttpContextAccessor _context;

		public AddOpreationInDbInterceptor(IHttpContextAccessor context)
		{
			_context = context;
		}


		private void HandleLogOperation(ChangeTracker changeTracker, DbContext context, string? userId, bool isUser)
		{

			var entities = changeTracker.Entries()
				.Where(entry =>
					(entry.State == EntityState.Added || entry.State == EntityState.Modified) &&
					entry.Entity is BaseEntity baseEntity &&
					baseEntity.DeletedAt == null);

			foreach (var entry in entities)
			{
				var baseEntity = (BaseEntity)entry.Entity;
		

				if (isUser)
				{
					var log = new UserOperationsLog
					{
						OperationType = Opreations.DeleteOpreation,
						UserId = userId,
						Description = $"{entry.State}_{baseEntity.GetType().Name} with ID: {baseEntity.Id}",
						Timestamp = DateTime.UtcNow,
						ItemId = baseEntity.Id
					};
					context.Add(log);

				}
				else
				{
					var log =
					new AdminOperationsLog
					{
						OperationType = Opreations.DeleteOpreation,
						AdminId = userId,
						Description = $"{entry.State} {baseEntity.GetType().Name} with ID: {baseEntity.Id}",
						Timestamp = DateTime.UtcNow,
						ItemId = baseEntity.Id
					};
					context.Add(log);
				}
			}
		}
		public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
		{
			if (eventData is null || eventData.Context is null || _context is null || _context.HttpContext is null) return new(result);


			string? userId = _context.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			var roles = _context.HttpContext.User.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value);
			bool isUser = roles.Contains("User");

			HandleLogOperation(eventData.Context.ChangeTracker, eventData.Context, userId, isUser);
			

			return new(result);
		}
		public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
		{
			if (eventData is null || eventData.Context is null || _context is null || _context.HttpContext is null) return result;


			string? userId = _context.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			var roles = _context.HttpContext.User.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value);
			bool isUser = roles.Contains("User");

			HandleLogOperation(eventData.Context.ChangeTracker, eventData.Context, userId, isUser);


			return result;
		}
	}

}