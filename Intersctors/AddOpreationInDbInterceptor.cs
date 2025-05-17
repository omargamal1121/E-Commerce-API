using E_Commers.Enums;
using E_Commers.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using System.Threading;

namespace E_Commers.Intersctors
{
	public class AddOperationInDbInterceptor : SaveChangesInterceptor
	{
		private readonly IHttpContextAccessor _contextAccessor;

		public AddOperationInDbInterceptor(IHttpContextAccessor contextAccessor)
		{
		
			_contextAccessor = contextAccessor;
		}

		private void HandleLogOperation(ChangeTracker changeTracker, DbContext context, string? userId, bool isUser)
		{
			var entities = changeTracker.Entries()
				.Where(entry =>
					(entry.State == EntityState.Added || entry.State == EntityState.Modified) &&
					entry.Entity is BaseEntity baseEntity &&
					baseEntity.DeletedAt == null&&entry.Entity is not AdminOperationsLog&& entry.Entity is not UserOperationsLog).ToList();

			foreach (var entry in entities)
			{
				var baseEntity = (BaseEntity)entry.Entity;
				var operationType = entry.State == EntityState.Added ? Opreations.AddOpreation : Opreations.UpdateOpreation;

				if (isUser)
				{
					var log = new UserOperationsLog
					{
						OperationType = operationType,
						UserId = userId,
						Description = $"{entry.State} {baseEntity.GetType().Name} with ID: {baseEntity.Id}",
						Timestamp = DateTime.UtcNow,
						ItemId = baseEntity.Id
					};
					context.Set<UserOperationsLog>().Add(log);

				}
				else
				{
					var log = new AdminOperationsLog
					{
						OperationType = operationType,
						AdminId = userId,
						Description = $"{entry.State} {baseEntity.GetType().Name} with ID: {baseEntity.Id}",
						Timestamp = DateTime.UtcNow,
						ItemId = baseEntity.Id
					};
					context.Set<AdminOperationsLog>().Add(log);
				}
			}
		}
		
		public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
		{
			if (eventData?.Context == null || _contextAccessor?.HttpContext == null)
				return base.SavingChangesAsync(eventData, result, cancellationToken);

			var userId = _contextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
				return base.SavingChangesAsync(eventData, result, cancellationToken);

			var isUser = _contextAccessor.HttpContext.User.IsInRole("User");
			HandleLogOperation(eventData.Context.ChangeTracker, eventData.Context, userId, isUser);


			return base.SavingChangesAsync(eventData, result, cancellationToken);
		}

		public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
		{
			if (eventData?.Context == null || _contextAccessor?.HttpContext == null)
				return base.SavingChanges(eventData, result);

			var userId = _contextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return base.SavingChanges(eventData, result);
			}

			var isUser = _contextAccessor.HttpContext.User.IsInRole("User");
			HandleLogOperation(eventData.Context.ChangeTracker, eventData.Context, userId, isUser);
				

			return base.SavingChanges(eventData, result);
		}
	}
}
