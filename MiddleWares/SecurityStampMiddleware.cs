using E_Commers.Models;
using Microsoft.AspNetCore.Identity;
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
					await context.Response.WriteAsync("{\"message\": \"Invalid Token - User ID missing\"}");
					await context.Response.WriteAsync("{\"statusCode\": 401}");
					return;
				}

				using (var scope = _serviceScopeFactory.CreateScope())
				{
					var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Customer>>();
					Customer? customer = await userManager.FindByIdAsync(userId);

					if (customer is null)
					{
						context.Response.StatusCode = 401;
						await context.Response.WriteAsync("{\n \"statusCode\": 401\n");
						await context.Response.WriteAsync(" \"message\": \"Invalid Token - User not found\"\n}");
						return;
					}

					string customerSecurityStamp = customer.SecurityStamp ?? string.Empty;
					string tokenSecurityStamp = jwtToken.Claims.FirstOrDefault(c => c.Type == "SecurityStamp")?.Value ?? string.Empty;

					if (string.IsNullOrEmpty(tokenSecurityStamp) || !tokenSecurityStamp.Equals(customerSecurityStamp))
					{
						context.Response.StatusCode = 401;
						await context.Response.WriteAsync("{\n \"statusCode\": 401\n");
						await context.Response.WriteAsync("    \"message\": \"Invalid Token - Security Stamp mismatch\"\n}");
						return;
					}
				}
			}
		}

		await _next(context);
	}
}
