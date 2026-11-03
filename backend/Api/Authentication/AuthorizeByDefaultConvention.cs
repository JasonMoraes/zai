using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Api.Authentication;

/// <summary>
/// Secure by default: an action that declares no authorization of its own (no <c>[Authorize…]</c> or
/// <c>[AllowAnonymous]</c> on it or its controller) requires an admin token, so a new modifying endpoint can't be
/// left open by forgetting an attribute.
/// <para>
/// Unlike <c>MapControllers().RequireAuthorization()</c>, this leaves actions with their own rule alone. ASP.NET
/// merges authorization metadata, so a global Bearer requirement combined with
/// <c>[Authorize(AuthenticationSchemes = "ApiKey")]</c> would let an admin token through where only a sensor key
/// may be accepted. Routing's own 404/405 responses aren't controller actions, so they stay public.
/// </para>
/// </summary>
public class AuthorizeByDefaultConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        foreach (var action in controller.Actions)
        {
            if (DeclaresAuthorization(controller.Attributes) || DeclaresAuthorization(action.Attributes))
                continue;

            foreach (var selector in action.Selectors)
                selector.EndpointMetadata.Add(new AuthorizeAttribute());
        }
    }

    private static bool DeclaresAuthorization(IEnumerable<object> attributes) =>
        attributes.Any(a => a is IAuthorizeData or IAllowAnonymous);
}
