using Hangfire.Dashboard;

namespace E_Commers
{
	public class AllowAllUsers : IDashboardAuthorizationFilter
	{
		public bool Authorize(DashboardContext context) => true;
	}

}