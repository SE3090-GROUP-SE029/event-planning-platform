using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.GuestManagement;

// Fail closed until the authentication module supplies a verified ClaimsPrincipal.
// Headers, route parameters, and request DTOs are never trusted as identity.
public class PlannerAccessFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
            context.Result = new UnauthorizedObjectResult(new { code = "authentication_required", error = "Planner authentication is required." });
        else if (!user.IsInRole("Planner") || string.IsNullOrWhiteSpace(user.FindFirstValue(ClaimTypes.NameIdentifier)))
            context.Result = new ObjectResult(new { code = "planner_required", error = "A planner identity is required." }) { StatusCode = 403 };
    }
}
