using Microsoft.AspNetCore.Mvc;

namespace PermitPro.Core.Filters
{
	public class RoleBasedAuthAttribute : TypeFilterAttribute
	{
		public RoleBasedAuthAttribute(string controller) : base(typeof(RoleBasedAuth))
		{
			var m = controller;
			Arguments = new object[] { controller };
		}
	}
}
