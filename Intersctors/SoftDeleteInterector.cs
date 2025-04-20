using E_Commers.Enums;
using E_Commers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace E_Commers.Interceptors
{
    public class SoftDeleteInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _context;

        public SoftDeleteInterceptor(IHttpContextAccessor context)
        {
            _context = context;
        }

        private void HandleSoftDelete(ChangeTracker changeTracker, DbContext context, string? userId, bool isUser)
        {
            var entities = changeTracker.Entries()
                .Where(entry => entry.State == EntityState.Deleted &&
                                entry.Entity is BaseEntity baseEntity &&
                                baseEntity.DeletedAt == null);

            foreach (var entry in entities)
            {
                var baseEntity = (BaseEntity)entry.Entity;
                entry.State = EntityState.Modified;
                baseEntity.DeletedAt = DateTime.UtcNow;

                 if (isUser){
                  var log=  new UserOperationsLog
                    {
                        OperationType = Opreations.DeleteOpreation,
                        UserId = userId,
                        Description = $"Deleted {baseEntity.GetType().Name} with ID: {baseEntity.Id}",
                        Timestamp = DateTime.UtcNow,
                        ItemId = baseEntity.Id
                    };
					context.Add(log);

				}
				else
				{
                    var log=
                    new AdminOperationsLog
                    {
                        OperationType = Opreations.DeleteOpreation,
                        AdminId = userId,
                        Description = $"Deleted {baseEntity.GetType().Name} with ID: {baseEntity.Id}",
                        Timestamp = DateTime.UtcNow,
                        ItemId = baseEntity.Id
                    };
                context.Add(log);
}
            }
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            if (eventData?.Context == null || _context?.HttpContext == null)
                return result;

            var user = _context.HttpContext.User;
            string? userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = user.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value);
            bool isUser = roles.Contains("User");

            HandleSoftDelete(eventData.Context.ChangeTracker, eventData.Context, userId, isUser);

            return result;
        }

        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (eventData?.Context == null || _context?.HttpContext == null)
                return new(result);

            var user = _context.HttpContext.User;
            string? userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = user.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value);
            bool isUser = roles.Contains("User");

            HandleSoftDelete(eventData.Context.ChangeTracker, eventData.Context, userId, isUser);

            return new(result);
        }
    }
}
