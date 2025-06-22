using E_Commers.Context;
using E_Commers.DtoModels.Responses;
using E_Commers.ErrorHnadling;
using E_Commers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class SecurityStampMiddleware
{
	private readonly RequestDelegate _next;
	private readonly IServiceScopeFactory _serviceScopeFactory;

	public SecurityStampMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory)
	{
		_next = next;
		_serviceScopeFactory = serviceScopeFactory;
	}

	public async Task Invoke(HttpContext context)
	{
		string? authHeader = context.Request.Headers["Authorization"];
		if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
		{
			string token = authHeader.Replace("Bearer ", "");
			var handler = new JwtSecurityTokenHandler();

			if (handler.CanReadToken(token))
			{
				var jwtToken = handler.ReadJwtToken(token);
				string? userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

				if (string.IsNullOrEmpty(userId))
				{
					context.Response.StatusCode = 401;
					context.Response.ContentType = "application/json";
					var response = ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Authentication", "Invalid Token - User ID missing"));
					await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
					return;
				}

				using var scope = _serviceScopeFactory.CreateScope();
				var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

				var customer = await dbContext.customers
					.Where(x => x.Id == userId)
					.Select(x => new Customer { Id = x.Id, SecurityStamp = x.SecurityStamp })
					.FirstOrDefaultAsync();

				if (customer is null)
				{
					context.Response.StatusCode = 401;
					context.Response.ContentType = "application/json";
					var response = ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Authentication", "Invalid Token - User not found"));
					await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
					return;
				}

				string tokenSecurityStamp = jwtToken.Claims.FirstOrDefault(c => c.Type == "SecurityStamp")?.Value ?? string.Empty;

				if (string.IsNullOrEmpty(customer.SecurityStamp) || tokenSecurityStamp != customer.SecurityStamp)
				{
					context.Response.StatusCode = 401;
					context.Response.ContentType = "application/json";
					var response = ApiResponse<string>.CreateErrorResponse(new ErrorResponse("Authentication", "Invalid Token - SecurityStamp mismatch"));
					await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
					return;
				}
			}
		}

		await _next(context);
	}

}
