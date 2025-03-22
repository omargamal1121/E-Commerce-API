using E_Commers.Models;
using Microsoft.AspNetCore.Identity;

public static class SeedData
{
	public static async Task SeedDataAsync(IServiceProvider serviceProvider)
	{
		var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
		var userManager = serviceProvider.GetRequiredService<UserManager<Customer>>();



		string adminEmail = "Omargamal1132004@example.com";
		string adminPassword = "Admin@123";

		var adminUser = await userManager.FindByEmailAsync(adminEmail);
		if (adminUser == null)
		{
			adminUser = new Customer
			{
				UserName = adminEmail,
				Email = adminEmail,
				EmailConfirmed = true
			};
			var result = await userManager.CreateAsync(adminUser, adminPassword);
			if (result.Succeeded)
			{
				await userManager.AddToRoleAsync(adminUser, "Admin");
			}
		}
	}
}
